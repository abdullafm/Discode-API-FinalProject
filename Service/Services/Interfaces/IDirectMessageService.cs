using Service.Helpers.DTOs.DirectMessages;

namespace Service.Services.Interfaces
{
    public interface IDirectMessageService
    {
        Task<IEnumerable<ConversationDto>> GetConversationsAsync(string userId);
        Task<ChatDto> GetChatAsync(string userId, string userName);
        Task<SentMessageDto> SendAsync(string senderId, string userName, SendMessageDto model);
        Task<EditedMessageDto> EditAsync(string userId, int messageId, SendMessageDto model);
        Task<ReactionUpdatedDto> ReactAsync(string userId, int messageId, ToggleReactionDto model);
        Task<PinUpdatedDto> PinAsync(string userId, int messageId, PinMessageDto model);
        Task<IEnumerable<MessageDto>> GetPinsAsync(string userId, string userName);
        Task MarkReadAsync(string userId, string userName);
        Task CloseAsync(string userId, string userName);
    }
}
