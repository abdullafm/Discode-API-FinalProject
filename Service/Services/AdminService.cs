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

        public AdminService(IAdminRepository admin)
        {
            _admin = admin;
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
