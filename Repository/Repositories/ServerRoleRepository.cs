using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerRoleRepository : BaseRepository<ServerRole>, IServerRoleRepository
    {
        public ServerRoleRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<ServerRole>> GetForServerAsync(int serverId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.MemberRoles)
                .Where(x => x.ServerId == serverId)
                .OrderByDescending(x => x.Position)
                .ToListAsync();
        }

        public async Task<ServerRole?> GetEveryoneAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.IsEveryone);
        }

        public async Task<ServerRole?> GetInServerAsync(int serverId, int roleId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.ServerId == serverId && x.Id == roleId);
        }

        public async Task<bool> NameExistsAsync(int serverId, string name, int? exceptId)
        {
            return await _dbSet.AnyAsync(x => x.ServerId == serverId && x.Name == name && (exceptId == null || x.Id != exceptId));
        }

        public async Task ShiftAboveEveryoneAsync(int serverId)
        {
            await _dbSet.Where(x => x.ServerId == serverId && !x.IsEveryone)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Position, x => x.Position + 1));
        }

        public async Task<int> NextPositionAsync(int serverId)
        {
            var positions = await _dbSet.Where(x => x.ServerId == serverId).Select(x => x.Position).ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max() + 1;
        }
    }
}
