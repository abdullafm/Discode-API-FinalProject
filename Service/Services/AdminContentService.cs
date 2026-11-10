using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AdminContentService : IAdminContentService
    {
        private readonly IAdminContentRepository _content;
        private readonly IPostRepository _posts;
        private readonly ICommentRepository _comments;
        private readonly IStoryRepository _stories;
        private readonly INotificationService _notifications;
        private readonly IUserModerationService _moderation;

        public AdminContentService(IAdminContentRepository content, IPostRepository posts, ICommentRepository comments, IStoryRepository stories,
                                   INotificationService notifications, IUserModerationService moderation)
        {
            _content = content;
            _posts = posts;
            _comments = comments;
            _stories = stories;
            _notifications = notifications;
            _moderation = moderation;
        }

        private static ReportTargetType Parse(string? type)
        {
            return (type ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "posts" or "post" => ReportTargetType.Post,
                "comments" or "comment" => ReportTargetType.Comment,
                "stories" or "story" => ReportTargetType.Story,
                _ => throw new BadRequestException("Choose posts, comments or stories.")
            };
        }

        public async Task<AdminContentListDto> GetAsync(string? type, string? search, int page, int pageSize)
        {
            var kind = Parse(string.IsNullOrWhiteSpace(type) ? "posts" : type);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var (rows, total) = await _content.GetPagedAsync(kind, search, page, pageSize);
            return new AdminContentListDto
            {
                Type = kind == ReportTargetType.Post ? "Posts" : kind == ReportTargetType.Comment ? "Comments" : "Stories",
                Total = total,
                Page = page,
                PageSize = pageSize,
                PostCount = await _content.CountAsync(ReportTargetType.Post),
                CommentCount = await _content.CountAsync(ReportTargetType.Comment),
                StoryCount = await _content.CountAsync(ReportTargetType.Story),
                Items = rows.Select(x => new AdminContentItemDto
                {
                    Id = x.Id,
                    Type = kind.ToString(),
                    PostId = x.PostId,
                    AuthorId = x.AuthorId,
                    AuthorUserName = x.AuthorUserName,
                    AuthorDisplayName = x.AuthorDisplayName,
                    AuthorAvatarUrl = x.AuthorAvatarUrl,
                    Text = x.Text,
                    MediaUrl = x.MediaUrl,
                    CreatedAt = x.CreatedAt,
                    Likes = x.Likes,
                    Comments = x.Comments,
                    Views = x.Views,
                    OpenReports = x.OpenReports
                }).ToList()
            };
        }

        public async Task<(string AuthorId, string Kind, string Snippet)> FindAsync(string type, int id)
        {
            switch (Parse(type))
            {
                case ReportTargetType.Post:
                {
                    var post = await _posts.GetByIdAsync(id) ?? throw new NotFoundException("That post is gone.");
                    return (post.AuthorId, "post", Clip(post.Text));
                }
                case ReportTargetType.Comment:
                {
                    var comment = await _comments.GetByIdAsync(id) ?? throw new NotFoundException("That comment is gone.");
                    return (comment.AuthorId, "comment", Clip(comment.Text));
                }
                default:
                {
                    var story = await _stories.GetByIdAsync(id) ?? throw new NotFoundException("That story is gone.");
                    return (story.AuthorId, "story", string.Empty);
                }
            }
        }

        public async Task<ContentRemoval> DeleteAsync(ModerationActor actor, string type, int id, string reason)
        {
            var kind = Parse(type);
            var (authorId, label, snippet) = await FindAsync(type, id);

            await _moderation.LogContentActionAsync(actor, authorId, AdminActionKind.ContentDelete, reason, label + " #" + id + (snippet.Length > 0 ? ": " + snippet : string.Empty));

            switch (kind)
            {
                case ReportTargetType.Post:
                    await _posts.DeleteAsync((await _posts.GetByIdAsync(id))!);
                    break;
                case ReportTargetType.Comment:
                    foreach (var item in await _comments.GetThreadAsync(id))
                    {
                        await _notifications.RemoveForCommentAsync(item.Id);
                        await _comments.DeleteAsync(item);
                    }
                    break;
                default:
                    await _stories.DeleteAsync((await _stories.GetByIdAsync(id))!);
                    break;
            }

            await _content.ResolveReportsAsync(kind, id.ToString(), actor.UserId);
            return new ContentRemoval(authorId, label, snippet);
        }

        private static string Clip(string? text)
        {
            text = (text ?? string.Empty).Replace("\n", " ").Trim();
            return text.Length <= 80 ? text : text.Substring(0, 80) + "...";
        }
    }
}
