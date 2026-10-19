using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Conversation?> GetBetweenAsync(string userIdA, string userIdB)
        {
            var first = string.CompareOrdinal(userIdA, userIdB) < 0 ? userIdA : userIdB;
            var second = first == userIdA ? userIdB : userIdA;

            return await _dbSet
                .Include(x => x.UserA)
                .Include(x => x.UserB)
                .FirstOrDefaultAsync(x => x.UserAId == first && x.UserBId == second);
        }

        public async Task<IEnumerable<Conversation>> GetForUserAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.UserA)
                .Include(x => x.UserB)
                .Where(x => ((x.UserAId == userId && !x.UserAHidden) || (x.UserBId == userId && !x.UserBHidden)) &&
                            !_dbContext.UserBlocks.Any(b =>
                                (b.BlockerId == userId && b.BlockedId == (x.UserAId == userId ? x.UserBId : x.UserAId)) ||
                                (b.BlockedId == userId && b.BlockerId == (x.UserAId == userId ? x.UserBId : x.UserAId))))
                .OrderByDescending(x => x.LastMessageAt)
                .ToListAsync();
        }
    }
}
