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

        public async Task<IEnumerable<string>> GetSharedSpaceIdsAsync(string userId)
        {
            var servers = _dbContext.ServerMembers.Where(x => x.UserId == userId).Select(x => x.ServerId);
            var groups = _dbContext.GroupMembers.Where(x => x.UserId == userId).Select(x => x.GroupId);
            var serverMates = _dbContext.ServerMembers.AsNoTracking().Where(x => servers.Contains(x.ServerId)).Select(x => x.UserId);
            var groupMates = _dbContext.GroupMembers.AsNoTracking().Where(x => groups.Contains(x.GroupId)).Select(x => x.UserId);
            return await serverMates.Union(groupMates).Where(id => id != userId).ToListAsync();
        }
    }
}
