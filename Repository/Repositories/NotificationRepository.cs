using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        private const int ListLimit = 60;

        public NotificationRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Notification>> GetByRecipientAsync(string recipientId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Actor)
                .Where(x => x.RecipientId == recipientId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(ListLimit)
                .ToListAsync();
        }

        public async Task<Notification?> FindAsync(NotificationType type, string actorId, string recipientId, int? postId, int? commentId, int? storyId, int? liveStreamId = null)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Type == type &&
                                                         x.ActorId == actorId &&
                                                         x.RecipientId == recipientId &&
                                                         x.PostId == postId &&
                                                         x.CommentId == commentId &&
                                                         x.StoryId == storyId &&
                                                         x.LiveStreamId == liveStreamId);
        }

        public async Task DeleteByCommentAsync(int commentId)
        {
            await _dbSet.Where(x => x.CommentId == commentId).ExecuteDeleteAsync();
        }

        public async Task MarkAllReadAsync(string recipientId)
        {
            await _dbSet
                .Where(x => x.RecipientId == recipientId && !x.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
        }
    }
}
