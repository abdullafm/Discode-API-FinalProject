using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.ServerMessages;

namespace Service.Services.Interfaces
{
    public interface IServerMessageService
    {
        Task<IEnumerable<ServerMessageDto>> GetMessagesAsync(string userId, int serverId, int channelId, int? beforeId);
        Task<ServerMessageChangeDto> SendAsync(string userId, int serverId, int channelId, SendServerMessageDto model);
        Task<ServerMessageChangeDto> SendPollAsync(string userId, int serverId, int channelId, CreatePollDto model);
        Task<ServerMessageUpdateDto> VotePollAsync(string userId, int serverId, int channelId, int messageId, VotePollDto model);
        Task<ServerMessageUpdateDto> EditAsync(string userId, int serverId, int channelId, int messageId, EditServerMessageDto model);
        Task<ServerMessageUpdateDto> PinAsync(string userId, int serverId, int channelId, int messageId, PinServerMessageDto model);
        Task<IEnumerable<ServerMessageDto>> GetPinsAsync(string userId, int serverId, int channelId);
        Task<ServerMessageDeletedDto> DeleteAsync(string userId, int serverId, int channelId, int messageId);
        Task<ServerMessageUpdateDto> ToggleReactionAsync(string userId, int serverId, int channelId, int messageId, ToggleReactionDto model);
    }
}
