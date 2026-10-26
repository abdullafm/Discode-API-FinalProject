using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerMessageRepository : BaseRepository<ServerMessage>, IServerMessageRepository
    {
        public ServerMessageRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        private IQueryable<ServerMessage> WithDetails()
        {
            return _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Sender)
                .Include(x => x.Attachments)
                .Include(x => x.Reactions)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender);
        }

        public async Task<IEnumerable<ServerMessage>> GetLatestAsync(int channelId, int count, int? beforeId = null)
        {
            var latest = await WithDetails()
                .Where(x => x.ChannelId == channelId && (beforeId == null || x.Id < beforeId))
                .OrderByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }

        public async Task<ServerMessage?> GetWithSenderAsync(int id)
        {
            return await WithDetails().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task ClearRepliesAsync(int messageId)
        {
            await _dbSet.Where(x => x.ReplyToMessageId == messageId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReplyToMessageId, (int?)null));
        }

        public async Task ToggleReactionAsync(int messageId, string userId, string emoji)
        {
            var existing = await _dbContext.Set<ServerMessageReaction>()
                .FirstOrDefaultAsync(x => x.ServerMessageId == messageId && x.UserId == userId && x.Emoji == emoji);
            if (existing is null)
            {
                _dbContext.Set<ServerMessageReaction>().Add(new ServerMessageReaction { ServerMessageId = messageId, UserId = userId, Emoji = emoji });
            }
            else
            {
                _dbContext.Set<ServerMessageReaction>().Remove(existing);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
