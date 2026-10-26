using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerMemberRepository : BaseRepository<ServerMember>, IServerMemberRepository
    {
        public ServerMemberRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<ServerMember>> GetForUserAsync(string userId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Position).ThenBy(x => x.JoinedAt).ToListAsync();
        }

        public async Task<int> NextPositionAsync(string userId)
        {
            var positions = await _dbSet.Where(x => x.UserId == userId).Select(x => x.Position).ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max() + 1;
        }

        public async Task<List<string>> GetUserIdsAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).Select(x => x.UserId).ToListAsync();
        }

        public async Task<ServerMember?> GetAsync(int serverId, string userId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.UserId == userId);
        }
    }
}
