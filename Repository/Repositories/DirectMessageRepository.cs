using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class DirectMessageRepository : IDirectMessageRepository
    {
        private readonly AppDbContext _dbContext;

        public DirectMessageRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(string userId, string otherId, int count)
        {
            var first = string.CompareOrdinal(userId, otherId) < 0 ? userId : otherId;
            var second = first == userId ? otherId : userId;

            var latest = await _dbContext.Set<Message>()
                .AsNoTracking()
                .Include(x => x.Sender)
                .Where(x => x.Conversation.UserAId == first && x.Conversation.UserBId == second)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }
    }
}
