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

        public async Task<IEnumerable<Message>> GetLatestAsync(int conversationId, int count)
        {
            var latest = await _dbSet
                .AsNoTracking()
                .Include(x => x.Sender)
                .Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }

        public async Task<Message?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(x => x.Sender)
                .Include(x => x.Conversation)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
