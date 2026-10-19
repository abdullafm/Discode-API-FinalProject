using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class StoryRepository : BaseRepository<Story>, IStoryRepository
    {
        public StoryRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Story>> GetVisibleAsync(string viewerId)
        {
            return await VisibleTo(viewerId)
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Likes)
                .Include(x => x.Views)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsVisibleAsync(string viewerId, int storyId)
        {
            return await VisibleTo(viewerId).AnyAsync(x => x.Id == storyId);
        }

        public async Task<int> DeleteExpiredAsync(DateTime cutoff)
        {
            return await _dbSet.Where(x => x.CreatedAt <= cutoff).ExecuteDeleteAsync();
        }

        private IQueryable<Story> VisibleTo(string viewerId)
        {
            var since = DateTime.UtcNow - StoryRules.Lifetime;

            return _dbSet.Where(x => x.CreatedAt > since &&
                                     (x.AuthorId == viewerId ||
                                      _dbContext.FriendRequests.Any(f =>
                                          f.Status == FriendRequestStatus.Accepted &&
                                          ((f.SenderId == viewerId && f.ReceiverId == x.AuthorId) ||
                                           (f.SenderId == x.AuthorId && f.ReceiverId == viewerId)))));
        }
    }
}
