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

        public async Task<Group?> GetTrackedWithMembersAsync(int id)
        {
            return await _dbSet
                .Include(x => x.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddMembersAsync(int groupId, IEnumerable<string> userIds)
        {
            foreach (var userId in userIds)
            {
                _dbContext.Set<GroupMember>().Add(new GroupMember { GroupId = groupId, UserId = userId });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(int groupId, string userId)
        {
            await _dbContext.Set<GroupMember>()
                .Where(x => x.GroupId == groupId && x.UserId == userId)
                .ExecuteDeleteAsync();
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
