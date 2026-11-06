using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IDiscoverRepository
    {
        Task<IEnumerable<Server>> GetPublicAsync(string? search, int take);
        Task<Server?> GetPublicAsync(int serverId);
        Task<IEnumerable<Server>> GetPublicForUserAsync(string userId);
    }
}
