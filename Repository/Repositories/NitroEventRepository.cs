using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class NitroEventRepository : BaseRepository<NitroEvent>, INitroEventRepository
    {
        public NitroEventRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> ExistsForStripeEventAsync(string stripeEventId)
        {
            return await _dbSet.AsNoTracking().AnyAsync(x => x.StripeEventId == stripeEventId);
        }

        public async Task<(List<NitroEvent> Items, int Total)> GetPageAsync(string? search, int page, int pageSize)
        {
            var query = _dbSet.AsNoTracking().Include(x => x.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => (x.User.UserName ?? string.Empty).Contains(term) || x.User.DisplayName.Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
