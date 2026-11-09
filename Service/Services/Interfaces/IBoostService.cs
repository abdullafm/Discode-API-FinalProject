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
        // the person lost Nitro: all their boosts go. Returns the servers that were boosted by them
        Task<List<int>> RemoveAllForUserAsync(string userId);
        // the server was deleted or banned: its boosts go. Each place stays locked until the day since the boost was placed is over
        Task ReleaseServerAsync(int serverId);
        // a super admin makes the wait on a person's boosts longer (hours above 0) or shorter (below 0)
        Task AdjustLockAsync(string userId, int hours);
        Task<bool> PerksUnlockedAsync(int serverId);
        Task<Dictionary<int, int>> GetCountsAsync(IEnumerable<int> serverIds);
        Task<HashSet<string>> GetBoosterIdsAsync(int serverId);
        // who among these people boosts any server (the Boost badge shows everywhere, not only on the server they boosted)
        Task<HashSet<string>> GetBoostersAmongAsync(IEnumerable<string> userIds);
        Task<bool> IsBoosterAsync(string userId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
