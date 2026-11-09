using Service.Helpers.DTOs.Nitro;

namespace Service.Services.Interfaces
{
    public interface IBoostService
    {
        Task<BoostInfoDto> GetInfoAsync(string userId, int serverId);
        Task<BoostChangeDto> BoostAsync(string userId, int serverId);
        Task<BoostChangeDto> UnboostAsync(string userId, int serverId);
        Task<BoostChangeDto> MoveAsync(string userId, MoveBoostDto model);
        Task<MyBoostsDto> GetMineAsync(string userId);
        Task<List<int>> RemoveAllForUserAsync(string userId);
        Task ReleaseServerAsync(int serverId);
        Task AdjustLockAsync(string userId, int hours);
        Task<bool> PerksUnlockedAsync(int serverId);
        Task<Dictionary<int, int>> GetCountsAsync(IEnumerable<int> serverIds);
        Task<HashSet<string>> GetBoosterIdsAsync(int serverId);
        Task<HashSet<string>> GetBoostersAmongAsync(IEnumerable<string> userIds);
        Task<bool> IsBoosterAsync(string userId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
