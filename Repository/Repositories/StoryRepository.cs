using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class StoryRepository : BaseRepository<Story>, IStoryRepository
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

        public StoryRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Story>> GetVisibleAsync(string viewerId)
        {
            return await VisibleTo(viewerId)
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Likes)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsVisibleAsync(string viewerId, int storyId)
        {
            return await VisibleTo(viewerId).AnyAsync(x => x.Id == storyId);
        }

        private IQueryable<Story> VisibleTo(string viewerId)
        {
            var since = DateTime.UtcNow - Lifetime;

            return _dbSet.Where(x => x.CreatedAt > since &&
                                     (x.AuthorId == viewerId ||
                                      _dbContext.FriendRequests.Any(f =>
                                          f.Status == FriendRequestStatus.Accepted &&
                                          ((f.SenderId == viewerId && f.ReceiverId == x.AuthorId) ||
                                           (f.SenderId == x.AuthorId && f.ReceiverId == viewerId)))));
        }
    }
}
