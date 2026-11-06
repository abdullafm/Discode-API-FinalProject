using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerRoleRepository : IBaseRepository<ServerRole>
    {
        Task<IEnumerable<ServerRole>> GetForServerAsync(int serverId);
        Task<ServerRole?> GetEveryoneAsync(int serverId);
        Task<ServerRole?> GetInServerAsync(int serverId, int roleId);
        Task<bool> NameExistsAsync(int serverId, string name, int? exceptId);
        Task<int> NextPositionAsync(int serverId);
        Task ShiftAboveEveryoneAsync(int serverId);
    }
}
