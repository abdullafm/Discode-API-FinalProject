using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerOverwriteRepository : BaseRepository<ServerOverwrite>, IServerOverwriteRepository
    {
        public ServerOverwriteRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerOverwrite>> GetForServerAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).OrderBy(x => x.Id).ToListAsync();
        }

        public async Task ReplaceForChannelAsync(int serverId, int channelId, List<ServerOverwrite> items)
        {
            await _dbSet.Where(x => x.ChannelId == channelId).ExecuteDeleteAsync();
            foreach (var item in items)
            {
                item.ServerId = serverId;
                item.ChannelId = channelId;
                item.CategoryId = null;
                _dbSet.Add(item);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceForCategoryAsync(int serverId, int categoryId, List<ServerOverwrite> items)
        {
            await _dbSet.Where(x => x.CategoryId == categoryId).ExecuteDeleteAsync();
            foreach (var item in items)
            {
                item.ServerId = serverId;
                item.CategoryId = categoryId;
                item.ChannelId = null;
                _dbSet.Add(item);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteForCategoryAsync(int categoryId)
        {
            await _dbSet.Where(x => x.CategoryId == categoryId).ExecuteDeleteAsync();
        }

        public async Task DeleteForRoleAsync(int roleId)
        {
            await _dbSet.Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
        }

        public async Task DeleteForServerAsync(int serverId)
        {
            await _dbSet.Where(x => x.ServerId == serverId).ExecuteDeleteAsync();
        }
    }
}
