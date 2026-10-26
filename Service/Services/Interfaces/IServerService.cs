using Service.Helpers.DTOs.Servers;

namespace Service.Services.Interfaces
{
    public interface IServerService
    {
        Task<ServerChangeDto> CreateAsync(string userId, CreateServerDto model);
        Task<ServerChangeDto> JoinAsync(string userId, JoinServerDto model);
        Task<IEnumerable<ServerDto>> GetMineAsync(string userId);
        Task<ServerDto> GetAsync(string userId, int serverId);
        Task<ServerInviteDto> GetInviteAsync(string userId, string invite);
        Task<ServerChangeDto> LeaveAsync(string userId, int serverId);
        Task<ServerDto> UpdatePreferencesAsync(string userId, int serverId, UpdateServerPreferencesDto model);
        Task<ServerChangeDto> UpdateProfileAsync(string userId, int serverId, UpdateServerProfileDto model);
        Task<ServerChangeDto> UpdateBannerAsync(string userId, int serverId, UpdateServerBannerDto model);
        Task<ServerChangeDto> UpdateInviteAsync(string userId, int serverId, UpdateServerInviteDto model);
        Task ReorderAsync(string userId, ReorderServersDto model);
    }
}
