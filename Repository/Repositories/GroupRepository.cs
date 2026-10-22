using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class GroupRepository : BaseRepository<Group>, IGroupRepository
    {
        public GroupRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Group?> GetWithMembersAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Group>> GetForUserAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Members).ThenInclude(m => m.User)
                .Where(x => x.Members.Any(m => m.UserId == userId))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Group?> FindWithSameMembersAsync(IReadOnlyCollection<string> userIds)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Members).ThenInclude(m => m.User)
                .Where(x => x.Members.Count == userIds.Count && x.Members.All(m => userIds.Contains(m.UserId)))
                .FirstOrDefaultAsync();
        }
    }
}
