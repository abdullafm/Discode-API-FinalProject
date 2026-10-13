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

        public Task<FriendRequest?> GetBetweenAsync(string userIdA, string userIdB)
            => _dbSet.FirstOrDefaultAsync(x =>
                (x.SenderId == userIdA && x.ReceiverId == userIdB) ||
                (x.SenderId == userIdB && x.ReceiverId == userIdA));

        public async Task<IReadOnlyList<FriendRequest>> GetSentPendingAsync(string senderId)
            => await _dbSet
                .AsNoTracking()
                .Include(x => x.Receiver)
                .Where(x => x.SenderId == senderId && x.Status == FriendRequestStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        public async Task<IReadOnlyList<FriendRequest>> GetReceivedPendingAsync(string receiverId)
            => await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Where(x => x.ReceiverId == receiverId && x.Status == FriendRequestStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        public async Task<IReadOnlyList<FriendRequest>> GetAcceptedAsync(string userId)
            => await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.Status == FriendRequestStatus.Accepted &&
                            (x.SenderId == userId || x.ReceiverId == userId))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
    }
}
