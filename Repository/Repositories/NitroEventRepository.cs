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

        private static bool IsPayment(NitroEvent e)
        {
            return e.AmountCents > 0 && (e.Kind == Domain.Enums.NitroEventKind.Subscribed || e.Kind == Domain.Enums.NitroEventKind.Renewed || e.Kind == Domain.Enums.NitroEventKind.GiftBought);
        }

        public async Task<List<NitroPaymentTotal>> GetPaymentTotalsAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<NitroPaymentTotal>();
            }

            var rows = await _dbSet.AsNoTracking()
                .Where(x => ids.Contains(x.UserId) && x.AmountCents > 0 &&
                            (x.Kind == Domain.Enums.NitroEventKind.Subscribed || x.Kind == Domain.Enums.NitroEventKind.Renewed || x.Kind == Domain.Enums.NitroEventKind.GiftBought))
                .Select(x => new { x.UserId, x.AmountCents, x.CreatedAt })
                .ToListAsync();
            return rows.GroupBy(x => x.UserId).Select(g => new NitroPaymentTotal
            {
                UserId = g.Key,
                Cents = g.Sum(x => (long)(x.AmountCents ?? 0)),
                Count = g.Count(),
                LastAt = g.Max(x => x.CreatedAt)
            }).ToList();
        }

        public async Task<(long Total, long Since)> GetRevenueAsync(DateTime since)
        {
            var rows = await _dbSet.AsNoTracking()
                .Where(x => x.AmountCents > 0 &&
                            (x.Kind == Domain.Enums.NitroEventKind.Subscribed || x.Kind == Domain.Enums.NitroEventKind.Renewed || x.Kind == Domain.Enums.NitroEventKind.GiftBought))
                .Select(x => new { x.AmountCents, x.CreatedAt })
                .ToListAsync();
            return (rows.Sum(x => (long)(x.AmountCents ?? 0)), rows.Where(x => x.CreatedAt >= since).Sum(x => (long)(x.AmountCents ?? 0)));
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
