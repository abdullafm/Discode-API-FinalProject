using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class EmojiCategoryRepository : BaseRepository<EmojiCategory>, IEmojiCategoryRepository
    {
        public EmojiCategoryRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<EmojiCategory>> GetAllWithEmojisAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Emojis.OrderBy(e => e.SortOrder))
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }
    }
}
