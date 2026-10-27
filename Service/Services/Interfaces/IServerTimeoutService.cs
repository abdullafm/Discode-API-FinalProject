using Service.Helpers.DTOs.Servers;

namespace Service.Services.Interfaces
{
    public interface IServerTimeoutService
    {
        Task SetAsync(string userId, int serverId, string targetUserId, TimeoutServerMemberDto model);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
