using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerChannelAccessRepository : IBaseRepository<ServerChannelAccess>
    {
        Task<List<ServerChannelAccess>> GetForChannelAsync(int channelId);
        Task<HashSet<int>> GetChannelIdsForAsync(int serverId, string userId);
        Task<bool> HasAccessAsync(int serverId, int channelId, string userId);
        Task ReplaceAsync(int channelId, IEnumerable<int> roleIds, IEnumerable<string> userIds);
        Task DeleteForChannelAsync(int channelId);
        Task DeleteForRoleAsync(int roleId);
    }
}
