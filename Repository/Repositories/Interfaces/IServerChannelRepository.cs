using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerChannelRepository : IBaseRepository<ServerChannel>
    {
        Task<List<ServerChannel>> GetLooseAsync(int serverId);
        Task<int> NextPositionAsync(int serverId, int? categoryId);
        Task<ServerChannel?> GetInServerAsync(int serverId, int channelId);
        Task<List<int>> GetIdsInCategoryAsync(int categoryId);
        Task<List<ServerChannel>> GetByCategoryAsync(int categoryId);
        Task DeleteInCategoryAsync(int categoryId);
        Task<bool> AnyAsync(int serverId);
    }
}
