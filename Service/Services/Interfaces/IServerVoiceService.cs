using Service.Helpers.DTOs.ServerVoice;

namespace Service.Services.Interfaces
{
    public interface IServerVoiceService
    {
        Task<ServerVoiceDto> GetAsync(string userId, int serverId);
        Task<ServerVoiceChangeDto> JoinAsync(string userId, int serverId, int channelId, JoinServerVoiceDto model);
        Task<ServerVoiceChangeDto> LeaveAsync(string userId);
        Task<ServerVoiceChangeDto> UpdateAsync(string userId, UpdateServerVoiceDto model);
        Task<ServerVoiceModerationDto> ModerateAsync(string actorId, int serverId, string targetUserId, ModerateServerVoiceDto model);
        Task<ServerVoiceModerationDto> DisconnectAsync(string actorId, int serverId, string targetUserId);
        Task<bool> AreTogetherAsync(string userId, string otherUserId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
