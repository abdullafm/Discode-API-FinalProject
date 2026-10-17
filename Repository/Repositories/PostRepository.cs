using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class PostRepository : BaseRepository<Post>, IPostRepository
    {
        private const int FeedLimit = 100;

        public PostRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Post>> GetExploreAsync(string viewerId)
        {
            return await _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Author)
                .Include(x => x.Likes)
                .Include(x => x.Comments).ThenInclude(c => c.Author)
                .Include(x => x.Comments).ThenInclude(c => c.Likes)
                .Where(x => x.Author.IsPublic &&
                            !_dbContext.UserBlocks.Any(b =>
                                (b.BlockerId == x.AuthorId && b.BlockedId == viewerId) ||
                                (b.BlockerId == viewerId && b.BlockedId == x.AuthorId)))
                .OrderByDescending(x => x.CreatedAt)
                .Take(FeedLimit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetFriendsFeedAsync(string viewerId)
        {
            return await _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Author)
                .Include(x => x.Likes)
                .Include(x => x.Comments).ThenInclude(c => c.Author)
                .Include(x => x.Comments).ThenInclude(c => c.Likes)
                .Where(x => x.AuthorId == viewerId ||
                            _dbContext.FriendRequests.Any(f =>
                                f.Status == FriendRequestStatus.Accepted &&
                                ((f.SenderId == viewerId && f.ReceiverId == x.AuthorId) ||
                                 (f.SenderId == x.AuthorId && f.ReceiverId == viewerId))))
                .OrderByDescending(x => x.CreatedAt)
                .Take(FeedLimit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByUserAsync(string viewerId, string authorId)
        {
            return await _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Author)
                .Include(x => x.Likes)
                .Include(x => x.Comments).ThenInclude(c => c.Author)
                .Include(x => x.Comments).ThenInclude(c => c.Likes)
                .Where(x => x.AuthorId == authorId &&
                            !_dbContext.UserBlocks.Any(b =>
                                (b.BlockerId == authorId && b.BlockedId == viewerId) ||
                                (b.BlockerId == viewerId && b.BlockedId == authorId)) &&
                            (viewerId == authorId ||
                             x.Author.IsPublic ||
                             _dbContext.FriendRequests.Any(f =>
                                 f.Status == FriendRequestStatus.Accepted &&
                                 ((f.SenderId == viewerId && f.ReceiverId == authorId) ||
                                  (f.SenderId == authorId && f.ReceiverId == viewerId)))))
                .OrderByDescending(x => x.CreatedAt)
                .Take(FeedLimit)
                .ToListAsync();
        }

        public async Task<bool> IsVisibleAsync(string viewerId, int postId)
        {
            return await _dbSet.AnyAsync(x => x.Id == postId &&
                                              !_dbContext.UserBlocks.Any(b =>
                                                  (b.BlockerId == x.AuthorId && b.BlockedId == viewerId) ||
                                                  (b.BlockerId == viewerId && b.BlockedId == x.AuthorId)) &&
                                              (x.AuthorId == viewerId ||
                                               x.Author.IsPublic ||
                                               _dbContext.FriendRequests.Any(f =>
                                                   f.Status == FriendRequestStatus.Accepted &&
                                                   ((f.SenderId == viewerId && f.ReceiverId == x.AuthorId) ||
                                                    (f.SenderId == x.AuthorId && f.ReceiverId == viewerId)))));
        }
    }
}
