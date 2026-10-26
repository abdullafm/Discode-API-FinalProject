using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerChannelAccessRepository : BaseRepository<ServerChannelAccess>, IServerChannelAccessRepository
    {
        public ServerChannelAccessRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<ServerChannelAccess>> GetForChannelAsync(int channelId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ChannelId == channelId).OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<HashSet<int>> GetChannelIdsForAsync(int serverId, string userId)
        {
            var ids = await Matching(serverId, userId).Select(x => x.ChannelId).Distinct().ToListAsync();
            return ids.ToHashSet();
        }

        public async Task<bool> HasAccessAsync(int serverId, int channelId, string userId)
        {
            return await Matching(serverId, userId).AnyAsync(x => x.ChannelId == channelId);
        }

        public async Task ReplaceAsync(int channelId, IEnumerable<int> roleIds, IEnumerable<string> userIds)
        {
            await _dbSet.Where(x => x.ChannelId == channelId).ExecuteDeleteAsync();
            foreach (var roleId in roleIds.Distinct())
            {
                _dbSet.Add(new ServerChannelAccess { ChannelId = channelId, RoleId = roleId });
            }

            foreach (var userId in userIds.Distinct())
            {
                _dbSet.Add(new ServerChannelAccess { ChannelId = channelId, UserId = userId });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteForChannelAsync(int channelId)
        {
            await _dbSet.Where(x => x.ChannelId == channelId).ExecuteDeleteAsync();
        }

        public async Task DeleteForRoleAsync(int roleId)
        {
            await _dbSet.Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
        }

        private IQueryable<ServerChannelAccess> Matching(int serverId, string userId)
        {
            return _dbSet.AsNoTracking().Where(a => a.Channel.ServerId == serverId
                && (a.UserId == userId
                    || (a.RoleId != null && _dbContext.ServerMemberRoles.Any(m => m.RoleId == a.RoleId
                        && m.ServerMember.ServerId == serverId
                        && m.ServerMember.UserId == userId))));
        }
    }
}
