using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CallRepository : BaseRepository<Call>, ICallRepository
    {
        public CallRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Call?> GetOpenByIdAsync(int id)
        {
            return await _dbSet
                .Include(x => x.Caller)
                .Include(x => x.Callee)
                .FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null);
        }

        public async Task<Call?> GetOpenBetweenAsync(string userId, string otherUserId)
        {
            return await _dbSet
                .Include(x => x.Caller)
                .Include(x => x.Callee)
                .Where(x => x.EndedAt == null &&
                            ((x.CallerId == userId && x.CalleeId == otherUserId) || (x.CallerId == otherUserId && x.CalleeId == userId)))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Call>> GetOpenForUserAsync(string userId)
        {
            return await _dbSet
                .Include(x => x.Caller)
                .Include(x => x.Callee)
                .Where(x => x.EndedAt == null && (x.CallerId == userId || x.CalleeId == userId))
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task EndExpiredRingingAsync(DateTime startedBefore)
        {
            var now = DateTime.UtcNow;
            await _dbSet
                .Where(x => x.EndedAt == null && !x.Active && x.CreatedAt < startedBefore)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now));
        }

        public async Task EndAllOpenAsync()
        {
            var now = DateTime.UtcNow;
            await _dbSet
                .Where(x => x.EndedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now));
        }
    }
}
