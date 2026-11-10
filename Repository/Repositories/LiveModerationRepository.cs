using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class LiveModerationRepository : ILiveModerationRepository
    {
        private readonly AppDbContext _dbContext;

        public LiveModerationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> IsModeratorAsync(string hostId, string userId)
        {
            return _dbContext.LiveModerators.AsNoTracking().AnyAsync(x => x.HostId == hostId && x.UserId == userId);
        }

        public async Task<HashSet<string>> GetModeratorIdsAsync(string hostId)
        {
            var ids = await _dbContext.LiveModerators.AsNoTracking().Where(x => x.HostId == hostId).Select(x => x.UserId).ToListAsync();
            return ids.ToHashSet();
        }

        public async Task AddModeratorAsync(string hostId, string userId)
        {
            if (await _dbContext.LiveModerators.AnyAsync(x => x.HostId == hostId && x.UserId == userId))
            {
                return;
            }

            _dbContext.LiveModerators.Add(new LiveModerator { HostId = hostId, UserId = userId });
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveModeratorAsync(string hostId, string userId)
        {
            var rows = await _dbContext.LiveModerators.Where(x => x.HostId == hostId && x.UserId == userId).ToListAsync();
            if (rows.Count == 0)
            {
                return;
            }

            _dbContext.LiveModerators.RemoveRange(rows);
            await _dbContext.SaveChangesAsync();
        }

        public Task<LiveChatMute?> GetActiveMuteAsync(string hostId, string userId, DateTime now)
        {
            return _dbContext.LiveChatMutes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.HostId == hostId && x.UserId == userId && (x.ExpiresAt == null || x.ExpiresAt > now));
        }

        public Task<List<LiveChatMute>> GetActiveMutesAsync(string hostId, DateTime now)
        {
            return _dbContext.LiveChatMutes.AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.HostId == hostId && (x.ExpiresAt == null || x.ExpiresAt > now))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task SetMuteAsync(string hostId, string userId, DateTime? expiresAt, string mutedById)
        {
            var row = await _dbContext.LiveChatMutes.FirstOrDefaultAsync(x => x.HostId == hostId && x.UserId == userId);
            if (row is null)
            {
                _dbContext.LiveChatMutes.Add(new LiveChatMute { HostId = hostId, UserId = userId, ExpiresAt = expiresAt, MutedById = mutedById });
            }
            else
            {
                row.ExpiresAt = expiresAt;
                row.MutedById = mutedById;
                row.CreatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> RemoveMuteAsync(string hostId, string userId)
        {
            var rows = await _dbContext.LiveChatMutes.Where(x => x.HostId == hostId && x.UserId == userId).ToListAsync();
            if (rows.Count == 0)
            {
                return false;
            }

            _dbContext.LiveChatMutes.RemoveRange(rows);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
