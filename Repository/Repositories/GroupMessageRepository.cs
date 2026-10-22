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

        private IQueryable<GroupMessage> WithDetails()
        {
            return _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Sender)
                .Include(x => x.Attachments)
                .Include(x => x.Reactions)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender)
                .Include(x => x.Poll)
                    .ThenInclude(p => p!.Options)
                        .ThenInclude(o => o.Votes);
        }

        public async Task<IEnumerable<GroupMessage>> GetLatestAsync(int groupId, int count)
        {
            var latest = await WithDetails()
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
            return await WithDetails().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task ClearRepliesAsync(int messageId)
        {
            await _dbSet.Where(x => x.ReplyToMessageId == messageId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReplyToMessageId, (int?)null));
        }

        public async Task ToggleReactionAsync(int messageId, string userId, string emoji)
        {
            var existing = await _dbContext.Set<GroupMessageReaction>()
                .FirstOrDefaultAsync(x => x.GroupMessageId == messageId && x.UserId == userId && x.Emoji == emoji);
            if (existing is null)
            {
                _dbContext.Set<GroupMessageReaction>().Add(new GroupMessageReaction { GroupMessageId = messageId, UserId = userId, Emoji = emoji });
            }
            else
            {
                _dbContext.Set<GroupMessageReaction>().Remove(existing);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
