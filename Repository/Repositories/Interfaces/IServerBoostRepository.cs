using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerBoostRepository : IBaseRepository<ServerBoost>
    {
        Task<int> CountForServerAsync(int serverId);
        Task<int> CountForUserAsync(string userId);
        Task<int> CountForUserOnServerAsync(string userId, int serverId);
        Task<Dictionary<int, int>> CountForServersAsync(IEnumerable<int> serverIds);
        Task<List<ServerBoost>> GetForUserAsync(string userId);
        Task<List<ServerBoost>> GetForUserOnServerAsync(string userId, int serverId);
        Task<List<string>> GetBoosterIdsAsync(int serverId);
        Task<List<ServerBoost>> GetForServerAsync(int serverId);
        Task<HashSet<string>> GetBoosterIdsAmongAsync(IEnumerable<string> userIds);
        Task DeleteRangeAsync(IEnumerable<ServerBoost> boosts);
    }
}
