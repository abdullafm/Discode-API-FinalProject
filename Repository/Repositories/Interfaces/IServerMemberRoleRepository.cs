using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerMemberRoleRepository : IBaseRepository<ServerMemberRole>
    {
        Task<IEnumerable<ServerMemberRole>> GetForServerAsync(int serverId);
        Task<IEnumerable<long>> GetPermissionBitsAsync(int serverId, string userId);
        Task<HashSet<int>> GetRoleIdsAsync(int serverId, string userId);
        Task<int> GetMaxPositionAsync(int serverId, string userId);
        Task<ServerMemberRole?> GetAsync(int roleId, int serverMemberId);
        Task DeleteForRoleAsync(int roleId);
    }
}
