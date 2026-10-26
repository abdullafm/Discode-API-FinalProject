using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerOverwriteRepository : IBaseRepository<ServerOverwrite>
    {
        Task<List<ServerOverwrite>> GetForServerAsync(int serverId);
        Task ReplaceForChannelAsync(int serverId, int channelId, List<ServerOverwrite> items);
        Task ReplaceForCategoryAsync(int serverId, int categoryId, List<ServerOverwrite> items);
        Task DeleteForCategoryAsync(int categoryId);
        Task DeleteForRoleAsync(int roleId);
        Task DeleteForServerAsync(int serverId);
    }
}
