using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerChannelRepository : IBaseRepository<ServerChannel>
    {
        Task<List<ServerChannel>> GetLooseAsync(int serverId);
        Task<int> NextPositionAsync(int serverId, int? categoryId);
        Task<bool> AnyAsync(int serverId);
    }
}
