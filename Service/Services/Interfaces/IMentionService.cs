using Domain.Enums;

namespace Service.Services.Interfaces
{
    public interface IMentionService
    {
        Task NotifyAsync(NotificationType type, string actorId, string text, int? postId = null, int? commentId = null);
    }
}
