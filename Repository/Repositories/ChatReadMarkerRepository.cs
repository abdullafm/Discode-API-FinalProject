using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ChatReadMarkerRepository : BaseRepository<ChatReadMarker>, IChatReadMarkerRepository
    {
        public ChatReadMarkerRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ChatReadMarker?> GetAsync(string userId, string kind, string key)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.UserId == userId && x.Kind == kind && x.Key == key);
        }
    }
}
