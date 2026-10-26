using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.ServerMessages;

namespace Service.Services.Interfaces
{
    public interface IServerMessageService
    {
        Task<IEnumerable<ServerMessageDto>> GetMessagesAsync(string userId, int serverId, int channelId, int? beforeId);
        Task<ServerMessageChangeDto> SendAsync(string userId, int serverId, int channelId, SendServerMessageDto model);
        Task<ServerMessageDeletedDto> DeleteAsync(string userId, int serverId, int channelId, int messageId);
        Task<ServerMessageUpdateDto> ToggleReactionAsync(string userId, int serverId, int channelId, int messageId, ToggleReactionDto model);
    }
}
