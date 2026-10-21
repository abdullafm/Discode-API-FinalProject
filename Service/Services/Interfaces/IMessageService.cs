using Service.Helpers.DTOs.DirectMessages;

namespace Service.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto?> GetAsync(int messageId, string viewerId);
        Task<MessageDto> CreateAsync(int conversationId, string senderId, string text, int? replyToMessageId);
        Task<MessageDto?> EditAsync(int messageId, string text, string viewerId);
        Task DeleteAsync(int messageId);
        Task<int?> GetLatestIdAsync(int conversationId);
        Task<IEnumerable<MessageDto>> GetPinnedAsync(int conversationId, string viewerId);
        Task<int> CountPinnedAsync(int conversationId);
        Task SetPinnedAsync(int messageId, bool pinned);
    }
}
