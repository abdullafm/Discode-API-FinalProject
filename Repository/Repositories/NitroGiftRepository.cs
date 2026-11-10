using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class NitroGiftRepository : INitroGiftRepository
    {
        private readonly AppDbContext _dbContext;

        public NitroGiftRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(NitroGift gift)
        {
            _dbContext.NitroGifts.Add(gift);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(NitroGift gift)
        {
            _dbContext.NitroGifts.Update(gift);
            await _dbContext.SaveChangesAsync();
        }

        private IQueryable<NitroGift> WithPeople()
        {
            return _dbContext.NitroGifts.Include(x => x.Buyer).Include(x => x.RedeemedBy);
        }

        public Task<NitroGift?> GetByCodeAsync(string code)
        {
            return WithPeople().FirstOrDefaultAsync(x => x.Code == code);
        }

        public Task<NitroGift?> GetByIdAsync(int id)
        {
            return WithPeople().FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<NitroGift?> GetByStripeSessionAsync(string sessionId)
        {
            return WithPeople().FirstOrDefaultAsync(x => x.StripeSessionId == sessionId);
        }

        public Task<List<NitroGift>> GetBoughtByAsync(string buyerId)
        {
            return WithPeople().AsNoTracking().Where(x => x.BuyerId == buyerId && !x.IsFree).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<(List<NitroGift> Items, int Total)> GetPageAsync(string? search, int page, int size)
        {
            var query = WithPeople().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Code.Contains(term) || (x.Buyer.UserName != null && x.Buyer.UserName.Contains(term)) || (x.RedeemedBy != null && x.RedeemedBy.UserName != null && x.RedeemedBy.UserName.Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CreatedAt).Skip((Math.Max(1, page) - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }
    }
}
