using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class SiteSettingRepository : ISiteSettingRepository
    {
        private readonly AppDbContext _dbContext;

        public SiteSettingRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<SiteSetting>> GetAllAsync() => _dbContext.SiteSettings.AsNoTracking().ToListAsync();

        public async Task SetAsync(string key, string value)
        {
            var row = await _dbContext.SiteSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (row is null)
            {
                _dbContext.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
            }
            else
            {
                row.Value = value;
                row.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveAsync(string key)
        {
            await _dbContext.SiteSettings.Where(x => x.Key == key).ExecuteDeleteAsync();
        }
    }
}
