using Service.Helpers.DTOs.ServerBans;

namespace Service.Services.Interfaces
{
    public interface IServerBanService
    {
        Task<IEnumerable<ServerBanDto>> GetBansAsync(string userId, int serverId, ServerBanFilterDto filter);
        Task<ServerBanDto> BanAsync(string userId, int serverId, BanServerMemberDto model);
        Task UnbanAsync(string userId, int serverId, string targetUserId);
        Task KickAsync(string userId, int serverId, string targetUserId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
