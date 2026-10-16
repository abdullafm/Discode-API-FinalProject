using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class PresenceRepository : IPresenceRepository
    {
        private readonly AppDbContext _dbContext;

        public PresenceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AppUser?> GetUserAsync(string userId)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> SetPreferredStatusAsync(string userId, PresenceStatus status)
        {
            var updated = await _dbContext.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PreferredStatus, status));

            return updated > 0;
        }

        public async Task<IEnumerable<string>> GetFriendIdsAsync(string userId)
        {
            return await _dbContext.FriendRequests
                .AsNoTracking()
                .Where(x => x.Status == FriendRequestStatus.Accepted &&
                            (x.SenderId == userId || x.ReceiverId == userId))
                .Select(x => x.SenderId == userId ? x.ReceiverId : x.SenderId)
                .ToListAsync();
        }
    }
}
