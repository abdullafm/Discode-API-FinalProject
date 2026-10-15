using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class UserBlockRepository : BaseRepository<UserBlock>, IUserBlockRepository
    {
        public UserBlockRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<UserBlock?> GetBlockAsync(string blockerId, string blockedId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.BlockerId == blockerId && x.BlockedId == blockedId);
        }

        public async Task<bool> IsBlockedEitherWayAsync(string userIdA, string userIdB)
        {
            return await _dbSet.AnyAsync(x =>
                (x.BlockerId == userIdA && x.BlockedId == userIdB) ||
                (x.BlockerId == userIdB && x.BlockedId == userIdA));
        }

        public async Task<IEnumerable<UserBlock>> GetBlockedByUserAsync(string blockerId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Blocked)
                .Where(x => x.BlockerId == blockerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
