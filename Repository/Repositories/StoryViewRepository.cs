using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class StoryViewRepository : BaseRepository<StoryView>, IStoryViewRepository
    {
        public StoryViewRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<StoryView?> GetByUserAsync(int storyId, string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.StoryId == storyId && x.UserId == userId);
        }

        public async Task<IEnumerable<StoryView>> GetByStoryAsync(int storyId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.StoryId == storyId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
