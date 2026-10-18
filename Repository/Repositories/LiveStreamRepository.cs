using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class LiveStreamRepository : BaseRepository<LiveStream>, ILiveStreamRepository
    {
        public LiveStreamRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<LiveStream>> GetActiveVisibleAsync(string viewerId)
        {
            return await VisibleTo(viewerId)
                .AsNoTracking()
                .Include(x => x.Host)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<LiveStream>> GetActiveByHostAsync(string hostId)
        {
            return await _dbSet
                .Where(x => x.HostId == hostId && x.EndedAt == null)
                .ToListAsync();
        }

        public async Task<LiveStream?> GetActiveVisibleByIdAsync(string viewerId, int streamId)
        {
            return await VisibleTo(viewerId)
                .AsNoTracking()
                .Include(x => x.Host)
                .FirstOrDefaultAsync(x => x.Id == streamId);
        }

        private IQueryable<LiveStream> VisibleTo(string viewerId)
        {
            return _dbSet.Where(x => x.EndedAt == null &&
                                     !_dbContext.UserBlocks.Any(b =>
                                         (b.BlockerId == x.HostId && b.BlockedId == viewerId) ||
                                         (b.BlockerId == viewerId && b.BlockedId == x.HostId)) &&
                                     (x.HostId == viewerId ||
                                      x.Host.IsPublic ||
                                      _dbContext.FriendRequests.Any(f =>
                                          f.Status == FriendRequestStatus.Accepted &&
                                          ((f.SenderId == viewerId && f.ReceiverId == x.HostId) ||
                                           (f.SenderId == x.HostId && f.ReceiverId == viewerId)))));
        }
    }
}
