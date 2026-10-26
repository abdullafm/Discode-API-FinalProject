using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerBanRepository : BaseRepository<ServerBan>, IServerBanRepository
    {
        public ServerBanRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerBan>> GetForServerAsync(int serverId, string? search, int? beforeId, int take)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.BannedBy)
                .Where(x => x.ServerId == serverId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var text = search.Trim();
                var upper = text.ToUpperInvariant();
                query = query.Where(x => x.UserId == text
                    || x.User.NormalizedUserName!.StartsWith(upper)
                    || x.User.DisplayName.StartsWith(text));
            }

            if (beforeId is not null)
            {
                query = query.Where(x => x.Id < beforeId);
            }

            return await query.OrderByDescending(x => x.Id).Take(take).ToListAsync();
        }

        public async Task<ServerBan?> GetAsync(int serverId, string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.ServerId == serverId && x.UserId == userId);
        }

        public async Task<bool> IsBannedAsync(int serverId, string userId)
        {
            return await _dbSet.AnyAsync(x => x.ServerId == serverId && x.UserId == userId);
        }
    }
}
