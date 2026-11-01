using Domain.Constants;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _admin;
        private readonly FluentValidation.IValidator<AdminListFilterDto> _filterValidator;

        public AdminService(IAdminRepository admin, FluentValidation.IValidator<AdminListFilterDto> filterValidator)
        {
            _admin = admin;
            _filterValidator = filterValidator;
        }

        public async Task<AdminUserListDto> GetUsersAsync(AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            var (users, total) = await _admin.GetUsersAsync(filter.Search, filter.Page, filter.PageSize);
            var ids = users.Select(x => x.Id).ToList();
            var roles = await _admin.GetRolesForUsersAsync(ids);
            var servers = await _admin.GetServerCountsForUsersAsync(ids);
            var bans = await _admin.GetBanCountsForUsersAsync(ids);

            return new AdminUserListDto
            {
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = users.Select(user => new AdminUserRowDto
                {
                    Id = user.Id,
                    PublicId = user.PublicId,
                    UserName = user.UserName ?? string.Empty,
                    DisplayName = user.DisplayName,
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = user.AvatarUrl,
                    IsPublic = user.IsPublic,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.TryGetValue(user.Id, out var list) ? list : new List<string>(),
                    ServersJoined = servers.TryGetValue(user.Id, out var joined) ? joined : 0,
                    BansReceived = bans.TryGetValue(user.Id, out var banned) ? banned : 0
                }).ToList()
            };
        }

        public async Task<AdminServerListDto> GetServersAsync(AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            var (servers, total) = await _admin.GetServersAsync(filter.Search, filter.Page, filter.PageSize);
            var ids = servers.Select(x => x.Id).ToList();
            var members = await _admin.GetMemberCountsForServersAsync(ids);
            var bans = await _admin.GetBanCountsForServersAsync(ids);

            return new AdminServerListDto
            {
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = servers.Select(server => new AdminServerRowDto
                {
                    Id = server.Id,
                    PublicId = server.PublicId,
                    Name = server.Name,
                    IconUrl = server.IconUrl,
                    OwnerUserName = server.Owner?.UserName ?? string.Empty,
                    IsPrivate = server.IsPrivate,
                    CreatedAt = server.CreatedAt,
                    Members = members.TryGetValue(server.Id, out var count) ? count : 0,
                    Bans = bans.TryGetValue(server.Id, out var banned) ? banned : 0
                }).ToList()
            };
        }

        public async Task<AdminOverviewDto> GetOverviewAsync()
        {
            var now = DateTime.UtcNow;
            var week = now.AddDays(-7);
            var month = now.AddDays(-30);

            var overview = new AdminOverviewDto();

            overview.Users = new AdminUserStatsDto
            {
                Total = await _admin.CountUsersAsync(),
                NewLast7Days = await _admin.CountUsersCreatedSinceAsync(week),
                NewLast30Days = await _admin.CountUsersCreatedSinceAsync(month),
                PublicAccounts = await _admin.CountUsersByVisibilityAsync(true),
                PrivateAccounts = await _admin.CountUsersByVisibilityAsync(false),
                Admins = await _admin.CountUsersInRoleAsync(Roles.Admin),
                SuperAdmins = await _admin.CountUsersInRoleAsync(Roles.SuperAdmin),
                Blocks = await _admin.CountUserBlocksAsync()
            };

            overview.Servers = new AdminServerStatsDto
            {
                Total = await _admin.CountServersAsync(),
                Public = await _admin.CountServersByPrivacyAsync(false),
                Private = await _admin.CountServersByPrivacyAsync(true),
                Memberships = await _admin.CountServerMembershipsAsync()
            };

            overview.Bans = new AdminBanStatsDto
            {
                TotalBans = await _admin.CountServerBansAsync(),
                BansLast7Days = await _admin.CountServerBansSinceAsync(week),
                UsersBanned = await _admin.CountDistinctBannedUsersAsync(),
                ServersWithBans = await _admin.CountServersWithBansAsync(),
                ActiveTimeouts = await _admin.CountActiveTimeoutsAsync(now)
            };

            overview.Content = new AdminContentStatsDto
            {
                Posts = await _admin.CountPostsAsync(),
                Comments = await _admin.CountCommentsAsync(),
                Reposts = await _admin.CountRepostsAsync(),
                Stories = await _admin.CountStoriesAsync(),
                Groups = await _admin.CountGroupsAsync(),
                DirectMessages = await _admin.CountDirectMessagesAsync(),
                GroupMessages = await _admin.CountGroupMessagesAsync(),
                ServerMessages = await _admin.CountServerMessagesAsync()
            };

            var reasons = await _admin.GetTopReportReasonsAsync(5);
            overview.Reports = new AdminReportStatsDto
            {
                Total = await _admin.CountReportsAsync(),
                Open = await _admin.CountReportsByStatusAsync(ReportStatus.Open),
                Reviewed = await _admin.CountReportsByStatusAsync(ReportStatus.Reviewed),
                Dismissed = await _admin.CountReportsByStatusAsync(ReportStatus.Dismissed),
                TopReasons = reasons.Select(x => new AdminReportReasonDto { Reason = x.Reason.ToString(), Count = x.Count }).ToList()
            };

            return overview;
        }
    }
}
