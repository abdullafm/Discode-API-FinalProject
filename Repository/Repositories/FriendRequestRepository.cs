using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class FriendRequestRepository : BaseRepository<FriendRequest>, IFriendRequestRepository
    {
        public FriendRequestRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<FriendRequest?> GetBetweenAsync(string userIdA, string userIdB)
        {
            return await _dbSet.FirstOrDefaultAsync(x =>
                (x.SenderId == userIdA && x.ReceiverId == userIdB) ||
                (x.SenderId == userIdB && x.ReceiverId == userIdA));
        }

        public async Task<IEnumerable<FriendRequest>> GetSentPendingAsync(string senderId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Receiver)
                .Where(x => x.SenderId == senderId && x.Status == FriendRequestStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FriendRequest>> GetReceivedPendingAsync(string receiverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == receiverId && x.Status == FriendRequestStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FriendRequest>> GetAcceptedAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.Status == FriendRequestStatus.Accepted &&
                            (x.SenderId == userId || x.ReceiverId == userId))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
