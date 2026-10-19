using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IStoryRepository : IBaseRepository<Story>
    {
        Task<IEnumerable<Story>> GetVisibleAsync(string viewerId);
        Task<bool> IsVisibleAsync(string viewerId, int storyId);
        Task<int> DeleteExpiredAsync(DateTime cutoff);
    }
}
