using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerCategoryRepository : BaseRepository<ServerCategory>, IServerCategoryRepository
    {
        public ServerCategoryRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerCategory>> GetForServerAsync(int serverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Channels)
                .Where(x => x.ServerId == serverId)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<ServerCategory?> GetInServerAsync(int serverId, int categoryId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.Id == categoryId);
        }

        public async Task<ServerCategory?> GetTrackedAsync(int serverId, int categoryId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.ServerId == serverId && x.Id == categoryId);
        }

        public async Task<int> NextPositionAsync(int serverId)
        {
            var positions = await _dbSet.Where(x => x.ServerId == serverId).Select(x => x.Position).ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max() + 1;
        }
    }
}
