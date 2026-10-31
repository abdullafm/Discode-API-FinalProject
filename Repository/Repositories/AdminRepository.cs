using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _dbContext;

        public AdminRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> CountUsersAsync() => _dbContext.Users.CountAsync();

        public Task<int> CountUsersCreatedSinceAsync(DateTime since) => _dbContext.Users.CountAsync(x => x.CreatedAt >= since);

        public Task<int> CountUsersByVisibilityAsync(bool isPublic) => _dbContext.Users.CountAsync(x => x.IsPublic == isPublic);

        public Task<int> CountUsersInRoleAsync(string roleName)
        {
            return (from userRole in _dbContext.UserRoles
                    join role in _dbContext.Roles on userRole.RoleId equals role.Id
                    where role.Name == roleName
                    select userRole.UserId).Distinct().CountAsync();
        }

        public Task<int> CountUserBlocksAsync() => _dbContext.UserBlocks.CountAsync();

        public Task<int> CountServersAsync() => _dbContext.Servers.CountAsync();

        public Task<int> CountServersByPrivacyAsync(bool isPrivate) => _dbContext.Servers.CountAsync(x => x.IsPrivate == isPrivate);

        public Task<int> CountServerMembershipsAsync() => _dbContext.ServerMembers.CountAsync();

        public Task<int> CountServerBansAsync() => _dbContext.ServerBans.CountAsync();

        public Task<int> CountServerBansSinceAsync(DateTime since) => _dbContext.ServerBans.CountAsync(x => x.CreatedAt >= since);

        public Task<int> CountDistinctBannedUsersAsync() => _dbContext.ServerBans.Select(x => x.UserId).Distinct().CountAsync();

        public Task<int> CountServersWithBansAsync() => _dbContext.ServerBans.Select(x => x.ServerId).Distinct().CountAsync();

        public Task<int> CountActiveTimeoutsAsync(DateTime now) => _dbContext.ServerMembers.CountAsync(x => x.TimeoutUntil != null && x.TimeoutUntil > now);

        public Task<int> CountPostsAsync() => _dbContext.Posts.CountAsync();

        public Task<int> CountCommentsAsync() => _dbContext.Comments.CountAsync();

        public Task<int> CountRepostsAsync() => _dbContext.PostReposts.CountAsync();

        public Task<int> CountStoriesAsync() => _dbContext.Stories.CountAsync();

        public Task<int> CountGroupsAsync() => _dbContext.Groups.CountAsync();

        public Task<int> CountDirectMessagesAsync() => _dbContext.Messages.CountAsync();

        public Task<int> CountGroupMessagesAsync() => _dbContext.GroupMessages.CountAsync();

        public Task<int> CountServerMessagesAsync() => _dbContext.ServerMessages.CountAsync();

        public Task<int> CountReportsAsync() => _dbContext.Reports.CountAsync();

        public Task<int> CountReportsByStatusAsync(ReportStatus status) => _dbContext.Reports.CountAsync(x => x.Status == status);

        public async Task<List<(ReportReason Reason, int Count)>> GetTopReportReasonsAsync(int take)
        {
            var rows = await _dbContext.Reports
                .GroupBy(x => x.Reason)
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(take)
                .ToListAsync();
            return rows.Select(x => (x.Reason, x.Count)).ToList();
        }
    }
}
