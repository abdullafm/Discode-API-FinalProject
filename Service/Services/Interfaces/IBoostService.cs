using Service.Helpers.DTOs.Nitro;

namespace Service.Services.Interfaces
{
    public interface IBoostService
    {
        Task<BoostInfoDto> GetInfoAsync(string userId, int serverId);
        Task<BoostChangeDto> BoostAsync(string userId, int serverId);
        Task<BoostChangeDto> UnboostAsync(string userId, int serverId);
        // the person lost Nitro: all their boosts go. Returns the servers that were boosted by them
        Task<List<int>> RemoveAllForUserAsync(string userId);
        Task<bool> PerksUnlockedAsync(int serverId);
        Task<Dictionary<int, int>> GetCountsAsync(IEnumerable<int> serverIds);
        Task<HashSet<string>> GetBoosterIdsAsync(int serverId);
        Task<bool> IsBoosterAsync(string userId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
