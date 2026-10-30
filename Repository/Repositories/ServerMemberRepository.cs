using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ServerMemberRepository : BaseRepository<ServerMember>, IServerMemberRepository
    {
        public ServerMemberRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<ServerMember>> GetForUserAsync(string userId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Position).ThenBy(x => x.JoinedAt).ToListAsync();
        }

        public async Task<int> NextPositionAsync(string userId)
        {
            var positions = await _dbSet.Where(x => x.UserId == userId).Select(x => x.Position).ToListAsync();
            return positions.Count == 0 ? 0 : positions.Max() + 1;
        }

        public async Task<List<string>> GetUserIdsAsync(int serverId)
        {
            return await _dbSet.AsNoTracking().Where(x => x.ServerId == serverId).Select(x => x.UserId).ToListAsync();
        }

        public async Task<List<ServerMember>> GetByUserIdsAsync(int serverId, List<string> userIds)
        {
            return await _dbSet.AsNoTracking().Include(x => x.User).Where(x => x.ServerId == serverId && userIds.Contains(x.UserId)).ToListAsync();
        }

        public async Task<Dictionary<string, int>> AddMentionAsync(int serverId, List<string> userIds)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            await _dbSet
                .Where(x => x.ServerId == serverId && userIds.Contains(x.UserId))
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.UnreadMentions, x => x.UnreadMentions + 1));

            return await _dbSet
                .AsNoTracking()
                .Where(x => x.ServerId == serverId && userIds.Contains(x.UserId))
                .ToDictionaryAsync(x => x.UserId, x => x.UnreadMentions);
        }

        public async Task ClearMentionsAsync(int serverId, string userId)
        {
            await _dbSet
                .Where(x => x.ServerId == serverId && x.UserId == userId && x.UnreadMentions != 0)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.UnreadMentions, 0));
        }

        public async Task<List<ServerMember>> GetPrunableAsync(int serverId, DateTime cutoff)
        {
            var ownerId = await _dbContext.Servers
                .Where(x => x.Id == serverId)
                .Select(x => x.OwnerId)
                .FirstOrDefaultAsync();

            return await _dbSet
                .AsNoTracking()
                .Where(m => m.ServerId == serverId
                    && m.JoinedAt < cutoff
                    && m.UserId != ownerId
                    && !_dbContext.ServerMemberRoles.Any(r => r.ServerMemberId == m.Id)
                    && !_dbContext.ServerMessages.Any(message => message.ServerId == serverId && message.SenderId == m.UserId && message.CreatedAt >= cutoff))
                .ToListAsync();
        }

        public async Task<List<ServerMember>> SearchAsync(int serverId, string? search, int take)
        {
            var query = _dbSet.AsNoTracking().Include(x => x.User).Where(x => x.ServerId == serverId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => (x.User.UserName != null && x.User.UserName.Contains(term)) || x.User.DisplayName.Contains(term));
            }

            return await query.OrderBy(x => x.User.UserName).Take(take).ToListAsync();
        }

        public async Task<ServerMember?> GetAsync(int serverId, string userId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == serverId && x.UserId == userId);
        }
    }
}
