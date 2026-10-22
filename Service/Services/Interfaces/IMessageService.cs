using Service.Helpers.DTOs.DirectMessages;

namespace Service.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto?> GetAsync(int messageId, string viewerId);
        Task<MessageDto> CreatePollAsync(int conversationId, string senderId, CreatePollDto model, bool undelivered);
        Task<MessageDto> CreateAsync(int conversationId, string senderId, string text, int? replyToMessageId, bool undelivered, IEnumerable<MessageAttachmentDto>? attachments = null);
        Task<MessageDto?> EditAsync(int messageId, string text, string viewerId);
        Task DeleteAsync(int messageId);
        Task<MessageDto> CreateCallAsync(int conversationId, string senderId);
        Task FinishCallAsync(int messageId, int? seconds, bool missed);
        Task<int?> GetLatestIdAsync(int conversationId);
        Task<IEnumerable<MessageDto>> GetPinnedAsync(int conversationId, string viewerId);
        Task<int> CountPinnedAsync(int conversationId);
        Task SetPinnedAsync(int messageId, bool pinned);
    }
}
