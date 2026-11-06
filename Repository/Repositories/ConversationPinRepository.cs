using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ConversationPinRepository : BaseRepository<ConversationPin>, IConversationPinRepository
    {
        public ConversationPinRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        // newest pin first, the same order the list shows
        public async Task<List<string>> GetKeysAsync(string userId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.Id).Select(x => x.Key).ToListAsync();
        }

        public async Task<ConversationPin?> GetAsync(string userId, string key)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Key == key);
        }
    }
}
