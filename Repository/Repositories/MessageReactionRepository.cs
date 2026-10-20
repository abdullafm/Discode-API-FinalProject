using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class MessageReactionRepository : BaseRepository<MessageReaction>, IMessageReactionRepository
    {
        public MessageReactionRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<MessageReaction?> GetByUserAsync(int messageId, string userId, string emoji)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.MessageId == messageId && x.UserId == userId && x.Emoji == emoji);
        }
    }
}
