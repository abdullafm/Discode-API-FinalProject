using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class NotificationService : INotificationService
    {
        private const int SnippetLength = 100;

        private readonly INotificationRepository _notifications;
        private readonly IMapper _mapper;

        public NotificationService(INotificationRepository notifications, IMapper mapper)
        {
            _notifications = notifications;
            _mapper = mapper;
        }

        public async Task AddAsync(NotificationType type, string actorId, string recipientId, int? postId = null, int? commentId = null, int? storyId = null, string? snippet = null)
        {
            if (actorId == recipientId)
            {
                return;
            }

            if (await _notifications.FindAsync(type, actorId, recipientId, postId, commentId, storyId) is not null)
            {
                return;
            }

            await _notifications.AddAsync(new Notification
            {
                Type = type,
                ActorId = actorId,
                RecipientId = recipientId,
                PostId = postId,
                CommentId = commentId,
                StoryId = storyId,
                Snippet = Shorten(snippet)
            });
        }

        public async Task RemoveAsync(NotificationType type, string actorId, string recipientId, int? postId = null, int? commentId = null, int? storyId = null)
        {
            var notification = await _notifications.FindAsync(type, actorId, recipientId, postId, commentId, storyId);
            if (notification is not null)
            {
                await _notifications.DeleteAsync(notification);
            }
        }

        public async Task RemoveForCommentAsync(int commentId)
        {
            await _notifications.DeleteByCommentAsync(commentId);
        }

        public async Task<IEnumerable<NotificationDto>> GetAsync(string userId)
        {
            var notifications = await _notifications.GetByRecipientAsync(userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task MarkAllReadAsync(string userId)
        {
            await _notifications.MarkAllReadAsync(userId);
        }

        public async Task MarkReadAsync(string userId, int notificationId)
        {
            var notification = await GetOwnedAsync(userId, notificationId);
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _notifications.UpdateAsync(notification);
            }
        }

        public async Task DeleteAsync(string userId, int notificationId)
        {
            var notification = await GetOwnedAsync(userId, notificationId);
            await _notifications.DeleteAsync(notification);
        }

        private async Task<Notification> GetOwnedAsync(string userId, int notificationId)
        {
            var notification = await _notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.RecipientId != userId)
            {
                throw new NotFoundException("Notification not found.");
            }

            return notification;
        }

        private static string? Shorten(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var value = text.Trim();
            return value.Length <= SnippetLength ? value : value.Substring(0, SnippetLength);
        }
    }
}
