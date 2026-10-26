using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerCategoryRepository : IBaseRepository<ServerCategory>
    {
        Task<List<ServerCategory>> GetForServerAsync(int serverId);
        Task<ServerCategory?> GetInServerAsync(int serverId, int categoryId);
        Task<ServerCategory?> GetTrackedAsync(int serverId, int categoryId);
        Task<int> NextPositionAsync(int serverId);
    }
}
