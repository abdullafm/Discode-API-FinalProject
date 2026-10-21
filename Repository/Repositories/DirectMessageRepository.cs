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
                .Include(x => x.Reactions)
<<<<<<< Updated upstream
=======
                .Include(x => x.Forward)
                .Include(x => x.ReplyTo)
                    .ThenInclude(r => r!.Sender)
>>>>>>> Stashed changes
                .Where(x => x.Conversation.UserAId == first && x.Conversation.UserBId == second)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(count)
                .ToListAsync();

            latest.Reverse();
            return latest;
        }

        public async Task<MessageForward> ForwardAsync(MessageForward forward)
        {
            _dbContext.Set<MessageForward>().Add(forward);
            await _dbContext.SaveChangesAsync();
            return forward;
        }
    }
}
