using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class StoryLikeRepository : BaseRepository<StoryLike>, IStoryLikeRepository
    {
        public StoryLikeRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<StoryLike?> GetByUserAsync(int storyId, string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.StoryId == storyId && x.UserId == userId);
        }

        public async Task<int> CountAsync(int storyId)
        {
            return await _dbSet.CountAsync(x => x.StoryId == storyId);
        }
    }
}
