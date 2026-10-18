using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IStoryLikeRepository : IBaseRepository<StoryLike>
    {
        Task<StoryLike?> GetByUserAsync(int storyId, string userId);
        Task<int> CountAsync(int storyId);
    }
}
