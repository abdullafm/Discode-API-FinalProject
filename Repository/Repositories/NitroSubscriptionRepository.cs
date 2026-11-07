using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class NitroSubscriptionRepository : BaseRepository<NitroSubscription>, INitroSubscriptionRepository
    {
        public NitroSubscriptionRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<NitroSubscription?> GetByUserAsync(string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<NitroSubscription?> GetByStripeSubscriptionAsync(string stripeSubscriptionId)
        {
            if (string.IsNullOrEmpty(stripeSubscriptionId))
            {
                return null;
            }

            return await _dbSet.FirstOrDefaultAsync(x => x.StripeSubscriptionId == stripeSubscriptionId);
        }

        public async Task<NitroSubscription?> GetByStripeCustomerAsync(string stripeCustomerId)
        {
            if (string.IsNullOrEmpty(stripeCustomerId))
            {
                return null;
            }

            return await _dbSet.FirstOrDefaultAsync(x => x.StripeCustomerId == stripeCustomerId);
        }

        public async Task<List<NitroSubscription>> GetOverdueAsync(DateTime now)
        {
            return await _dbSet.Where(x => x.Status != NitroStatus.Ended && x.CurrentPeriodEnd <= now).ToListAsync();
        }

        public async Task<HashSet<string>> GetActiveUserIdsAsync(IEnumerable<string> userIds, DateTime now)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new HashSet<string>();
            }

            var found = await _dbSet.AsNoTracking()
                .Where(x => ids.Contains(x.UserId) && x.Status != NitroStatus.Ended && x.CurrentPeriodEnd > now)
                .Select(x => x.UserId)
                .ToListAsync();
            return found.ToHashSet();
        }

        public async Task<(List<NitroSubscription> Items, int Total)> GetPageAsync(string? search, bool activeOnly, DateTime now, int page, int pageSize)
        {
            var query = _dbSet.AsNoTracking().Include(x => x.User).AsQueryable();
            if (activeOnly)
            {
                query = query.Where(x => x.Status != NitroStatus.Ended && x.CurrentPeriodEnd > now);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => (x.User.UserName ?? string.Empty).Contains(term) || x.User.DisplayName.Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CurrentPeriodEnd).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
