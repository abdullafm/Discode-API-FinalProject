using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class DiscoverRepository : IDiscoverRepository
    {
        private readonly AppDbContext _dbContext;

        public DiscoverRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Server>> GetPublicAsync(string? search, int take)
        {
            var query = Public();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.Description.Contains(term));
            }

            return await query
                .OrderByDescending(x => x.Members.Count)
                .ThenBy(x => x.Name)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Server?> GetPublicAsync(int serverId)
        {
            return await Public().FirstOrDefaultAsync(x => x.Id == serverId);
        }

        public async Task<IEnumerable<Server>> GetPublicForUserAsync(string userId)
        {
            return await Public()
                .Where(x => x.Members.Any(m => m.UserId == userId))
                .ToListAsync();
        }

        private IQueryable<Server> Public()
        {
            return _dbContext.Servers
                .AsNoTracking()
                .Include(x => x.Members).ThenInclude(m => m.User)
                .Where(x => !x.IsPrivate);
        }
    }
}
