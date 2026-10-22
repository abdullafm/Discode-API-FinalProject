using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class GroupMessageRepository : BaseRepository<GroupMessage>, IGroupMessageRepository
    {
        public GroupMessageRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<GroupMessage>> GetLatestAsync(int groupId, int count)
        {
            var latest = await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Where(x => x.GroupId == groupId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }

        public async Task<GroupMessage?> GetWithSenderAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
