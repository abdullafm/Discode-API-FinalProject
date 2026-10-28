using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class PollRepository : BaseRepository<Poll>, IPollRepository
    {
        public PollRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Poll?> GetByMessageIdAsync(int messageId)
        {
            return await _dbSet
                .Include(x => x.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(x => x.MessageId == messageId);
        }

        public async Task<Poll?> GetByGroupMessageIdAsync(int groupMessageId)
        {
            return await _dbSet
                .Include(x => x.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(x => x.GroupMessageId == groupMessageId);
        }

        public async Task<Poll?> GetByServerMessageIdAsync(int serverMessageId)
        {
            return await _dbSet
                .Include(x => x.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(x => x.ServerMessageId == serverMessageId);
        }

        public async Task SetVotesAsync(Poll poll, string userId, IReadOnlyCollection<int> optionIds)
        {
            var optionKeys = poll.Options.Select(o => o.Id).ToList();
            var mine = await _dbContext.Set<PollVote>()
                .Where(v => optionKeys.Contains(v.OptionId) && v.UserId == userId)
                .ToListAsync();

            _dbContext.Set<PollVote>().RemoveRange(mine.Where(v => !optionIds.Contains(v.OptionId)));

            var kept = mine.Select(v => v.OptionId).ToHashSet();
            foreach (var optionId in optionIds.Where(id => !kept.Contains(id)))
            {
                _dbContext.Set<PollVote>().Add(new PollVote { OptionId = optionId, UserId = userId });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
