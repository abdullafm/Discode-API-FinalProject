using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task ClearRepliesToAsync(int messageId)
        {
            await _dbSet.Where(x => x.ReplyToId == messageId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReplyToId, (int?)null));
        }

        public async Task<IEnumerable<Message>> GetLatestAsync(int conversationId, int count)
        {
            var latest = await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Reactions)
                .Include(x => x.Forward)
                .Include(x => x.Attachments)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender)
                .Where(x => x.ConversationId == conversationId && !x.Undelivered)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }

        public async Task<IEnumerable<Message>> GetPinnedAsync(int conversationId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Reactions)
                .Include(x => x.Forward)
                .Include(x => x.Attachments)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender)
                .Where(x => x.ConversationId == conversationId && x.PinnedAt != null)
                .OrderByDescending(x => x.PinnedAt)
                .ToListAsync();
        }

        public async Task<int> CountPinnedAsync(int conversationId)
        {
            return await _dbSet.CountAsync(x => x.ConversationId == conversationId && x.PinnedAt != null);
        }

        public async Task<Message?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(x => x.Sender)
                .Include(x => x.Reactions)
                .Include(x => x.Forward)
                .Include(x => x.Attachments)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender)
                .Include(x => x.Conversation)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
