using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerRepository : BaseRepository<Server>, IServerRepository
    {
        public ServerRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Server?> GetWithMembersAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Server?> GetByInviteCodeAsync(string inviteCode)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(x => x.InviteCode == inviteCode);
        }

        public async Task<IEnumerable<Server>> GetForUserAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Members)
                .Where(x => x.Members.Any(m => m.UserId == userId))
                .OrderBy(x => x.Members.Where(m => m.UserId == userId).Select(m => m.Position).FirstOrDefault())
                .ThenBy(x => x.Members.Where(m => m.UserId == userId).Select(m => m.JoinedAt).FirstOrDefault())
                .ToListAsync();
        }

        public async Task<bool> InviteCodeExistsAsync(string inviteCode)
        {
            return await _dbSet.AnyAsync(x => x.InviteCode == inviteCode);
        }
    }
}
