using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerBoostRepository : BaseRepository<ServerBoost>, IServerBoostRepository
    {
        public ServerBoostRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<int> CountForServerAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().CountAsync(x => x.ServerId == serverId);
        }

        public async Task<int> CountForUserAsync(string userId)
        {
            return await _dbSet.AsNoTracking().CountAsync(x => x.UserId == userId);
        }

        public async Task<int> CountForUserOnServerAsync(string userId, int serverId)
        {
            return await _dbSet.AsNoTracking().CountAsync(x => x.UserId == userId && x.ServerId == serverId);
        }

        public async Task<Dictionary<int, int>> CountForServersAsync(IEnumerable<int> serverIds)
        {
            var ids = serverIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            return await _dbSet.AsNoTracking()
                .Where(x => ids.Contains(x.ServerId))
                .GroupBy(x => x.ServerId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<List<ServerBoost>> GetForUserAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<List<ServerBoost>> GetForUserOnServerAsync(string userId, int serverId)
        {
            return await _dbSet.Where(x => x.UserId == userId && x.ServerId == serverId).OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<HashSet<string>> GetBoosterIdsAmongAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new HashSet<string>();
            }

            var found = await _dbSet.AsNoTracking().Where(x => ids.Contains(x.UserId)).Select(x => x.UserId).Distinct().ToListAsync();
            return found.ToHashSet();
        }

        public async Task<List<ServerBoost>> GetForServerAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Include(x => x.User).Where(x => x.ServerId == serverId).OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<List<string>> GetBoosterIdsAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).Select(x => x.UserId).Distinct().ToListAsync();
        }

        public async Task DeleteRangeAsync(IEnumerable<ServerBoost> boosts)
        {
            // the rows are looked up again so a copy that is already tracked is the one that is removed
            var ids = boosts.Select(b => b.Id).ToList();
            _dbSet.RemoveRange(await _dbSet.Where(x => ids.Contains(x.Id)).ToListAsync());
            await _dbContext.SaveChangesAsync();
        }
    }
}
