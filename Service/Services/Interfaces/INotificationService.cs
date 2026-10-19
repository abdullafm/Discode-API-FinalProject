using Domain.Enums;
using Service.Helpers.DTOs.Notifications;

namespace Service.Services.Interfaces
{
    public interface INotificationService
    {
        Task AddAsync(NotificationType type, string actorId, string recipientId, int? postId = null, int? commentId = null, int? storyId = null, string? snippet = null);
        Task RemoveAsync(NotificationType type, string actorId, string recipientId, int? postId = null, int? commentId = null, int? storyId = null);
        Task RemoveForCommentAsync(int commentId);
        Task<IEnumerable<NotificationDto>> GetAsync(string userId);
        Task MarkAllReadAsync(string userId);
        Task MarkReadAsync(string userId, int notificationId);
        Task DeleteAsync(string userId, int notificationId);
    }
}
