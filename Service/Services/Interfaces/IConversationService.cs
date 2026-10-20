using Service.Helpers.DTOs.DirectMessages;

namespace Service.Services.Interfaces
{
    public interface IConversationService
    {
        Task<IEnumerable<ConversationDto>> GetForUserAsync(string userId);
        Task<ConversationDto?> GetBetweenAsync(string userId, string otherId);
        Task<ConversationDto?> GetByIdAsync(int conversationId, string viewerId);
        Task<ConversationDto> CreateAsync(string userId, string otherId, string lastMessageText, bool hiddenForOther);
        Task ShowForAsync(string userId, string otherId);
        Task HideForAsync(string userId, string otherId);
        Task ResetUnreadAsync(string userId, string otherId);
        Task RecordMessageAsync(string senderId, string recipientId, string text, DateTime sentAt);
        Task UpdatePreviewAsync(int conversationId, string text);
    }
}
