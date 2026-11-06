using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AdminContentRepository : IAdminContentRepository
    {
        private readonly AppDbContext _db;

        public AdminContentRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<int> CountAsync(ReportTargetType type)
        {
            return type switch
            {
                ReportTargetType.Post => _db.Posts.CountAsync(),
                ReportTargetType.Comment => _db.Comments.CountAsync(),
                ReportTargetType.Story => _db.Stories.CountAsync(),
                _ => Task.FromResult(0)
            };
        }

        public async Task<(List<AdminContentRow> Items, int Total)> GetPagedAsync(ReportTargetType type, string? search, int page, int pageSize)
        {
            var term = (search ?? string.Empty).Trim();
            var open = ReportStatus.Open;

            if (type == ReportTargetType.Post)
            {
                var query = _db.Posts.AsNoTracking().AsQueryable();
                if (term.Length > 0)
                {
                    query = query.Where(x => x.Text.Contains(term) || x.Author.UserName!.Contains(term) || x.Author.DisplayName.Contains(term));
                }

                var total = await query.CountAsync();
                var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new AdminContentRow(x.Id, null, x.AuthorId, x.Author.UserName ?? string.Empty, x.Author.DisplayName, x.Author.AvatarUrl,
                        x.Text, x.ImageUrl, x.CreatedAt, x.Likes.Count, x.Comments.Count, 0,
                        _db.Reports.Count(r => r.TargetType == ReportTargetType.Post && r.TargetId == x.Id.ToString() && r.Status == open)))
                    .ToListAsync();
                return (rows, total);
            }

            if (type == ReportTargetType.Comment)
            {
                var query = _db.Comments.AsNoTracking().AsQueryable();
                if (term.Length > 0)
                {
                    query = query.Where(x => x.Text.Contains(term) || x.Author.UserName!.Contains(term) || x.Author.DisplayName.Contains(term));
                }

                var total = await query.CountAsync();
                var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new AdminContentRow(x.Id, x.PostId, x.AuthorId, x.Author.UserName ?? string.Empty, x.Author.DisplayName, x.Author.AvatarUrl,
                        x.Text, string.Empty, x.CreatedAt, x.Likes.Count, 0, 0,
                        _db.Reports.Count(r => r.TargetType == ReportTargetType.Comment && r.TargetId == x.Id.ToString() && r.Status == open)))
                    .ToListAsync();
                return (rows, total);
            }

            var stories = _db.Stories.AsNoTracking().AsQueryable();
            if (term.Length > 0)
            {
                stories = stories.Where(x => x.Author.UserName!.Contains(term) || x.Author.DisplayName.Contains(term));
            }

            var storyTotal = await stories.CountAsync();
            var storyRows = await stories.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new AdminContentRow(x.Id, null, x.AuthorId, x.Author.UserName ?? string.Empty, x.Author.DisplayName, x.Author.AvatarUrl,
                    string.Empty, x.ImageUrl, x.CreatedAt, x.Likes.Count, 0, x.Views.Count,
                    _db.Reports.Count(r => r.TargetType == ReportTargetType.Story && r.TargetId == x.Id.ToString() && r.Status == open)))
                .ToListAsync();
            return (storyRows, storyTotal);
        }

        // reports about content that was just removed are closed together with it
        public async Task<int> ResolveReportsAsync(ReportTargetType type, string targetId, string adminId)
        {
            var now = DateTime.UtcNow;
            return await _db.Reports
                .Where(x => x.TargetType == type && x.TargetId == targetId && x.Status == ReportStatus.Open)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.Status, ReportStatus.Reviewed)
                    .SetProperty(x => x.ReviewedById, adminId)
                    .SetProperty(x => x.ReviewedAt, now));
        }
    }
}
