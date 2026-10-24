using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByRecipientAsync(string recipientId);
        Task<Notification?> FindAsync(NotificationType type, string actorId, string recipientId, int? postId, int? commentId, int? storyId, int? liveStreamId = null);
        Task DeleteByCommentAsync(int commentId);
        Task MarkAllReadAsync(string recipientId);
    }
}
