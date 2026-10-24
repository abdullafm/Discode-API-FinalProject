using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class UserIgnoreRepository : BaseRepository<UserIgnore>, IUserIgnoreRepository
    {
        public UserIgnoreRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<UserIgnore?> GetAsync(string ignorerId, string ignoredId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.IgnorerId == ignorerId && x.IgnoredId == ignoredId);
        }

        public async Task<IEnumerable<UserIgnore>> GetIgnoredByUserAsync(string ignorerId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Ignored)
                .Where(x => x.IgnorerId == ignorerId)
                .ToListAsync();
        }
    }
}
