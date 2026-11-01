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

        public Task<int> CountServersAsync() => _dbContext.Servers.CountAsync();

        public Task<int> CountServersByPrivacyAsync(bool isPrivate) => _dbContext.Servers.CountAsync(x => x.IsPrivate == isPrivate);

        public Task<int> CountServerBansAsync() => _dbContext.ServerBans.CountAsync();

        public Task<int> CountServerBansSinceAsync(DateTime since) => _dbContext.ServerBans.CountAsync(x => x.CreatedAt >= since);

        public Task<int> CountDistinctBannedUsersAsync() => _dbContext.ServerBans.Select(x => x.UserId).Distinct().CountAsync();

        public Task<int> CountServersWithBansAsync() => _dbContext.ServerBans.Select(x => x.ServerId).Distinct().CountAsync();

        public Task<int> CountActiveTimeoutsAsync(DateTime now) => _dbContext.ServerMembers.CountAsync(x => x.TimeoutUntil != null && x.TimeoutUntil > now);

        public Task<int> CountPostsAsync() => _dbContext.Posts.CountAsync();

        public Task<int> CountCommentsAsync() => _dbContext.Comments.CountAsync();

        public async Task<(List<Domain.Entities.AppUser> Users, int Total)> GetUsersAsync(string? search, string? role, bool bannedOnly, int page, int pageSize)
        {
            var query = _dbContext.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(user => (from userRole in _dbContext.UserRoles
                                             join r in _dbContext.Roles on userRole.RoleId equals r.Id
                                             where r.Name == role && userRole.UserId == user.Id
                                             select userRole.UserId).Any());
            }

            if (bannedOnly)
            {
                query = query.Where(user => _dbContext.ServerBans.Any(ban => ban.UserId == user.Id));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => (x.UserName != null && x.UserName.Contains(term)) || x.DisplayName.Contains(term) || (x.Email != null && x.Email.Contains(term)));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (users, total);
        }

        public async Task<Dictionary<string, List<string>>> GetRolesForUsersAsync(List<string> userIds)
        {
            var rows = await (from userRole in _dbContext.UserRoles
                              join role in _dbContext.Roles on userRole.RoleId equals role.Id
                              where userIds.Contains(userRole.UserId)
                              select new { userRole.UserId, role.Name }).ToListAsync();
            return rows.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? string.Empty).ToList());
        }

        public async Task<Dictionary<string, int>> GetServerCountsForUsersAsync(List<string> userIds)
        {
            return await _dbContext.ServerMembers
                .Where(x => userIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetBanCountsForUsersAsync(List<string> userIds)
        {
            return await _dbContext.ServerBans
                .Where(x => userIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public Task<(List<Domain.Entities.Server> Servers, int Total)> GetAllServersAsync(bool bannedOnly, int page, int pageSize)
        {
            return QueryServersAsync(null, bannedOnly, page, pageSize);
        }

        public Task<(List<Domain.Entities.Server> Servers, int Total)> SearchServersAsync(string term, bool bannedOnly, int page, int pageSize)
        {
            return QueryServersAsync(term.Trim(), bannedOnly, page, pageSize);
        }

        private async Task<(List<Domain.Entities.Server> Servers, int Total)> QueryServersAsync(string? term, bool bannedOnly, int page, int pageSize)
        {
            var query = _dbContext.Servers.AsNoTracking().Include(x => x.Owner).AsQueryable();
            if (bannedOnly)
            {
                query = query.Where(server => _dbContext.ServerBans.Any(ban => ban.ServerId == server.Id));
            }

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(x => x.Name.Contains(term) || (x.Owner.UserName != null && x.Owner.UserName.Contains(term)));
            }

            var total = await query.CountAsync();
            var servers = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (servers, total);
        }

        public async Task<Dictionary<int, int>> GetMemberCountsForServersAsync(List<int> serverIds)
        {
            return await _dbContext.ServerMembers
                .Where(x => serverIds.Contains(x.ServerId))
                .GroupBy(x => x.ServerId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<Dictionary<int, int>> GetBanCountsForServersAsync(List<int> serverIds)
        {
            return await _dbContext.ServerBans
                .Where(x => serverIds.Contains(x.ServerId))
                .GroupBy(x => x.ServerId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

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
