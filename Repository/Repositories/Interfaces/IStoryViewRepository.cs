using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IStoryViewRepository : IBaseRepository<StoryView>
    {
        Task<StoryView?> GetByUserAsync(int storyId, string userId);
        Task<IEnumerable<StoryView>> GetByStoryAsync(int storyId);
    }
}
