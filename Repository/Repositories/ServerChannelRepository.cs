using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerChannelRepository : BaseRepository<ServerChannel>, IServerChannelRepository
    {
        public ServerChannelRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerChannel>> GetLooseAsync(int serverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.ServerId == serverId && x.CategoryId == null)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<int> NextPositionAsync(int serverId, int? categoryId)
        {
            var positions = await _dbSet.Where(x => x.ServerId == serverId && x.CategoryId == categoryId).Select(x => x.Position).ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max() + 1;
        }

        public async Task<bool> AnyAsync(int serverId)
        {
            return await _dbSet.AnyAsync(x => x.ServerId == serverId);
        }
    }
}
