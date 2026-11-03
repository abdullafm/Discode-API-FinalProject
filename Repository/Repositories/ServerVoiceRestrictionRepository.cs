using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerVoiceRestrictionRepository : BaseRepository<ServerVoiceRestriction>, IServerVoiceRestrictionRepository
    {
        public ServerVoiceRestrictionRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ServerVoiceRestriction?> GetAsync(int serverId, string userId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.UserId == userId);
        }

        public async Task<List<ServerVoiceRestriction>> GetForServerAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).ToListAsync();
        }
    }
}
