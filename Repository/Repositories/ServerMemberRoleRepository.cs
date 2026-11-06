using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerMemberRoleRepository : BaseRepository<ServerMemberRole>, IServerMemberRoleRepository
    {
        public ServerMemberRoleRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<ServerMemberRole>> GetForServerAsync(int serverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.ServerMember)
                .Where(x => x.Role.ServerId == serverId)
                .ToListAsync();
        }

        public async Task<IEnumerable<long>> GetPermissionBitsAsync(int serverId, string userId)
        {
            return await _dbSet
                .Where(x => x.ServerMember.ServerId == serverId && x.ServerMember.UserId == userId)
                .Select(x => x.Role.Permissions)
                .ToListAsync();
        }

        public async Task<HashSet<int>> GetRoleIdsAsync(int serverId, string userId)
        {
            var ids = await _dbSet
                .Where(x => x.ServerMember.ServerId == serverId && x.ServerMember.UserId == userId)
                .Select(x => x.RoleId)
                .ToListAsync();
            return ids.ToHashSet();
        }

        public async Task<int> GetMaxPositionAsync(int serverId, string userId)
        {
            var positions = await _dbSet
                .Where(x => x.ServerMember.ServerId == serverId && x.ServerMember.UserId == userId)
                .Select(x => x.Role.Position)
                .ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max();
        }

        public async Task<ServerMemberRole?> GetAsync(int roleId, int serverMemberId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.RoleId == roleId && x.ServerMemberId == serverMemberId);
        }

        public async Task DeleteForRoleAsync(int roleId)
        {
            await _dbSet.Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
        }
    }
}
