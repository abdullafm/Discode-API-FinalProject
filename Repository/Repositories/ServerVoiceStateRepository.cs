using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerVoiceStateRepository : BaseRepository<ServerVoiceState>, IServerVoiceStateRepository
    {
        public ServerVoiceStateRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ServerVoiceState?> GetForUserAsync(string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<List<ServerVoiceState>> GetForServerAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).OrderBy(x => x.CreatedAt).ToListAsync();
        }

        public async Task DeleteForUserInServerAsync(int serverId, string userId)
        {
            await _dbSet.Where(x => x.ServerId == serverId && x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
