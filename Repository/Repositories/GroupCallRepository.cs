using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class GroupCallRepository : BaseRepository<GroupCall>, IGroupCallRepository
    {
        public GroupCallRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        private IQueryable<GroupCall> Full()
        {
            return _dbSet
                .AsNoTracking()
                .Include(x => x.Group)
                .Include(x => x.Starter)
                .Include(x => x.Participants)
                    .ThenInclude(p => p.User);
        }

        public async Task<GroupCall?> GetFullAsync(int id)
        {
            return await Full().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<GroupCall?> GetActiveForGroupAsync(int groupId)
        {
            return await Full().Where(x => x.GroupId == groupId && x.Active).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<GroupCall>> GetActiveForGroupsAsync(IReadOnlyCollection<int> groupIds)
        {
            return await Full().Where(x => x.Active && groupIds.Contains(x.GroupId)).ToListAsync();
        }

        public async Task<IEnumerable<GroupCall>> GetActiveForUserAsync(string userId)
        {
            return await Full()
                .Where(x => x.Active && x.Participants.Any(p => p.UserId == userId && p.LeftAt == null))
                .ToListAsync();
        }

        public async Task SetParticipantAsync(int callId, string userId, bool inCall)
        {
            var set = _dbContext.Set<GroupCallParticipant>();
            var row = await set.FirstOrDefaultAsync(p => p.GroupCallId == callId && p.UserId == userId);
            if (row is null)
            {
                if (!inCall)
                {
                    return;
                }

                set.Add(new GroupCallParticipant { GroupCallId = callId, UserId = userId, JoinedAt = DateTime.UtcNow });
            }
            else
            {
                row.Screen = false;
                row.LeftAt = inCall ? null : DateTime.UtcNow;
                if (inCall)
                {
                    row.JoinedAt = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task SetScreenAsync(int callId, string userId, bool on)
        {
            await _dbContext.Set<GroupCallParticipant>()
                .Where(p => p.GroupCallId == callId && p.UserId == userId && p.LeftAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Screen, on));
        }

        public async Task EndAsync(int callId)
        {
            var now = DateTime.UtcNow;
            await _dbContext.Set<GroupCallParticipant>()
                .Where(p => p.GroupCallId == callId && p.LeftAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LeftAt, now).SetProperty(p => p.Screen, false));
            await _dbSet
                .Where(x => x.Id == callId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Active, false).SetProperty(x => x.EndedAt, now));
        }

        public async Task EndAllOpenAsync()
        {
            var open = await _dbSet.Where(x => x.Active).Select(x => x.Id).ToListAsync();
            foreach (var id in open)
            {
                await EndAsync(id);
            }
        }
    }
}
