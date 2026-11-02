using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ReportRepository : BaseRepository<Report>, IReportRepository
    {
        private readonly IStoryRepository _stories;

        public ReportRepository(AppDbContext dbContext, IStoryRepository stories) : base(dbContext)
        {
            _stories = stories;
        }

        public async Task<(List<ReportRow> Items, int Total)> GetPagedAsync(ReportStatus? status, string? search, int page, int pageSize)
        {
            var query = _dbContext.Reports.AsNoTracking().AsQueryable();
            if (status is not null)
            {
                query = query.Where(x => x.Status == status);
            }

            var term = (search ?? string.Empty).Trim();
            if (term.Length > 0)
            {
                query = query.Where(x => x.Reporter.UserName!.Contains(term)
                    || x.Details.Contains(term)
                    || x.ContentSnapshot.Contains(term)
                    || _dbContext.Users.Any(u => u.Id == x.TargetOwnerId && (u.UserName!.Contains(term) || u.DisplayName.Contains(term))));
            }

            var total = await query.CountAsync();
            var rows = await query
                .OrderBy(x => x.Status == ReportStatus.Open ? 0 : 1)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReportRow(
                    x,
                    x.Reporter.UserName ?? string.Empty, x.Reporter.DisplayName, x.Reporter.AvatarUrl,
                    _dbContext.Users.Where(u => u.Id == x.TargetOwnerId).Select(u => u.UserName).FirstOrDefault(),
                    _dbContext.Users.Where(u => u.Id == x.TargetOwnerId).Select(u => u.DisplayName).FirstOrDefault(),
                    _dbContext.Users.Where(u => u.Id == x.TargetOwnerId).Select(u => u.AvatarUrl).FirstOrDefault(),
                    _dbContext.Servers.Where(s => s.Id == x.ContextServerId).Select(s => s.Name).FirstOrDefault(),
                    _dbContext.Groups.Where(g => g.Id == x.ContextGroupId).Select(g => g.Name).FirstOrDefault(),
                    _dbContext.Users.Where(u => u.Id == x.ReviewedById).Select(u => u.UserName).FirstOrDefault()))
                .ToListAsync();

            return (rows, total);
        }

        public async Task<bool> HasOpenReportAsync(string reporterId, ReportTargetType type, string targetId)
        {
            return await _dbSet.AnyAsync(x => x.ReporterId == reporterId && x.TargetType == type && x.TargetId == targetId && x.Status == ReportStatus.Open);
        }

        public async Task<ReportTarget?> ResolveAsync(string reporterId, ReportTargetType type, string targetId)
        {
            var id = 0;
            if (type != ReportTargetType.User && !int.TryParse(targetId, out id))
            {
                return null;
            }

            switch (type)
            {
                case ReportTargetType.User:
                {
                    var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetId || x.UserName == targetId);
                    if (user is null) return null;
                    return new ReportTarget(user.Id, Clip($"@{user.UserName} ({user.DisplayName})\n{user.Bio}"), null, null);
                }
                case ReportTargetType.Server:
                {
                    var server = await _dbContext.Servers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (server is null) return null;
                    if (!await _dbContext.ServerMembers.AnyAsync(x => x.ServerId == id && x.UserId == reporterId)) return null;
                    return new ReportTarget(server.OwnerId, Clip($"{server.Name}\n{server.Description}"), server.Id, null);
                }
                case ReportTargetType.Group:
                {
                    var group = await _dbContext.Groups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (group is null) return null;
                    if (!await _dbContext.GroupMembers.AnyAsync(x => x.GroupId == id && x.UserId == reporterId)) return null;
                    return new ReportTarget(group.OwnerId, Clip(group.Name), null, group.Id);
                }
                case ReportTargetType.DirectMessage:
                {
                    var message = await _dbContext.Messages.AsNoTracking().Include(x => x.Conversation).FirstOrDefaultAsync(x => x.Id == id);
                    if (message is null) return null;
                    if (message.Conversation.UserAId != reporterId && message.Conversation.UserBId != reporterId) return null;
                    return new ReportTarget(message.SenderId, Clip(message.Text), null, null);
                }
                case ReportTargetType.GroupMessage:
                {
                    var message = await _dbContext.GroupMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (message is null || message.IsSystem) return null;
                    if (!await _dbContext.GroupMembers.AnyAsync(x => x.GroupId == message.GroupId && x.UserId == reporterId)) return null;
                    return new ReportTarget(message.SenderId, Clip(message.Text), null, message.GroupId);
                }
                case ReportTargetType.ServerMessage:
                {
                    var message = await _dbContext.ServerMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (message is null) return null;
                    if (!await _dbContext.ServerMembers.AnyAsync(x => x.ServerId == message.ServerId && x.UserId == reporterId)) return null;
                    return new ReportTarget(message.SenderId, Clip(message.Text), message.ServerId, null);
                }
                case ReportTargetType.Story:
                {
                    var story = await _dbContext.Stories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (story is null) return null;
                    if (story.AuthorId != reporterId && !await _stories.IsVisibleAsync(reporterId, id)) return null;
                    return new ReportTarget(story.AuthorId, Clip(story.ImageUrl), null, null);
                }
                case ReportTargetType.Post:
                {
                    var post = await _dbContext.Posts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (post is null) return null;
                    return new ReportTarget(post.AuthorId, Clip(string.IsNullOrEmpty(post.ImageUrl) ? post.Text : post.Text + "\n" + post.ImageUrl), null, null);
                }
                case ReportTargetType.Comment:
                {
                    var comment = await _dbContext.Comments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (comment is null) return null;
                    return new ReportTarget(comment.AuthorId, Clip(comment.Text), null, null);
                }
                default:
                    return null;
            }
        }

        private static string Clip(string? text)
        {
            text ??= string.Empty;
            return text.Length <= 2000 ? text : text[..2000];
        }
    }
}
