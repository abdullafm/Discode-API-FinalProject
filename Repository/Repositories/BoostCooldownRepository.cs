using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class BoostCooldownRepository : BaseRepository<BoostCooldown>, IBoostCooldownRepository
    {
        public BoostCooldownRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<BoostCooldown>> GetActiveAsync(string userId, DateTime now)
        {
            return await _dbSet.AsNoTracking().Where(x => x.UserId == userId && x.AvailableAt > now).OrderBy(x => x.AvailableAt).ToListAsync();
        }

        public async Task DeleteForUserAsync(string userId, DateTime? onlyBefore = null)
        {
            var rows = await _dbSet.Where(x => x.UserId == userId && (onlyBefore == null || x.AvailableAt <= onlyBefore)).ToListAsync();
            if (rows.Count == 0)
            {
                return;
            }

            _dbSet.RemoveRange(rows);
            await _dbContext.SaveChangesAsync();
        }
    }
}
