using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class UserModerationRepository : IUserModerationRepository
    {
        private readonly AppDbContext _dbContext;

        public UserModerationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<AppUser?> GetTrackedUserAsync(string userId)
        {
            return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

        public async Task AddWarningAsync(UserWarning warning)
        {
            await _dbContext.UserWarnings.AddAsync(warning);
        }

        public async Task AddActionAsync(AdminAction action)
        {
            await _dbContext.AdminActions.AddAsync(action);
        }

        public Task<int> CountWarningsAsync(string userId) => _dbContext.UserWarnings.CountAsync(x => x.UserId == userId);

        public async Task<List<(UserWarning Warning, string IssuedBy)>> GetWarningsAsync(string userId, int take)
        {
            var rows = await (from warning in _dbContext.UserWarnings.AsNoTracking()
                              join admin in _dbContext.Users on warning.IssuedById equals admin.Id into admins
                              from admin in admins.DefaultIfEmpty()
                              where warning.UserId == userId
                              orderby warning.CreatedAt descending
                              select new { warning, name = admin != null ? admin.UserName : null })
                .Take(take)
                .ToListAsync();
            return rows.Select(x => (x.warning, x.name ?? string.Empty)).ToList();
        }

        public async Task<List<AdminAction>> GetActionsForTargetAsync(AdminTargetType type, string targetId, int take)
        {
            return await _dbContext.AdminActions
                .AsNoTracking()
                .Include(x => x.Admin)
                .Where(x => x.TargetType == type && x.TargetId == targetId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<(List<AdminAction> Actions, int Total)> GetActionsAsync(string? search, int page, int pageSize)
        {
            var query = _dbContext.AdminActions.AsNoTracking().Include(x => x.Admin).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.TargetName.Contains(term) || x.Reason.Contains(term) || (x.Admin.UserName != null && x.Admin.UserName.Contains(term)));
            }

            var total = await query.CountAsync();
            var actions = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (actions, total);
        }

        public Task<int> CountReportsAgainstAsync(string userId) => _dbContext.Reports.CountAsync(x => x.TargetOwnerId == userId);

        public Task<int> CountPostsByAsync(string userId) => _dbContext.Posts.CountAsync(x => x.AuthorId == userId);

        public Task<int> CountCommentsByAsync(string userId) => _dbContext.Comments.CountAsync(x => x.AuthorId == userId);

        public async Task<UserStatusRow?> GetStatusAsync(string userId)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new UserStatusRow(x.IsBanned, x.BannedUntil, x.BanReason, x.ForcedLogoutAt, x.TimeoutUntil, x.TimeoutReason, x.IsDeleted))
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserWarning>> GetUnseenWarningsAsync(string userId)
        {
            return await _dbContext.UserWarnings
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.SeenAt == null)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkWarningSeenAsync(string userId, int warningId)
        {
            await _dbContext.UserWarnings
                .Where(x => x.Id == warningId && x.UserId == userId && x.SeenAt == null)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.SeenAt, DateTime.UtcNow));
        }
    }
}
