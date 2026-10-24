using Service.Helpers.DTOs.DirectMessages;

namespace Service.Services.Interfaces
{
    public interface IDirectMessageService
    {
        Task<IEnumerable<ConversationDto>> GetConversationsAsync(string userId);
        Task<ChatDto> GetChatAsync(string userId, string userName);
        Task<RelationDto> GetRelationAsync(string userId, string userName);
        Task<SentMessageDto> SendAsync(string senderId, string userName, SendMessageDto model);
        Task<SentMessageDto> SendStoryReplyAsync(string senderId, int storyId, string text);
        Task<SentMessageDto> SendLiveShareAsync(string senderId, string userName, LiveShareDto live);
        Task<SentMessageDto> SendPollAsync(string senderId, string userName, CreatePollDto model);
        Task<PollUpdatedDto> VotePollAsync(string userId, int messageId, VotePollDto model);
        Task<EditedMessageDto> EditAsync(string userId, int messageId, SendMessageDto model);
        Task<IEnumerable<SentMessageDto>> ForwardAsync(string userId, int messageId, ForwardMessageDto model);
        Task<IEnumerable<SentMessageDto>> ForwardContentAsync(string userId, string originalSenderId, string text, IEnumerable<MessageAttachmentDto> attachments, ForwardMessageDto model);
        Task<SentMessageDto> StartCallMessageAsync(string callerId, string calleeId);
        Task<EditedMessageDto> FinishCallMessageAsync(string callerId, string calleeId, int messageId, int? seconds, bool missed);
        Task<DeletedMessageDto> DeleteAsync(string userId, int messageId);
        Task<ReactionUpdatedDto> ReactAsync(string userId, int messageId, ToggleReactionDto model);
        Task<PinUpdatedDto> PinAsync(string userId, int messageId, PinMessageDto model);
        Task<IEnumerable<MessageDto>> GetPinsAsync(string userId, string userName);
        Task MarkReadAsync(string userId, string userName);
        Task CloseAsync(string userId, string userName);
    }
}
