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
        private readonly FluentValidation.IValidator<BanServerDto> _banValidator;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IServerOverwriteRepository _overwrites;

        public AdminService(IAdminRepository admin,
                            FluentValidation.IValidator<AdminListFilterDto> filterValidator,
                            FluentValidation.IValidator<BanServerDto> banValidator,
                            IServerRepository servers,
                            IServerMemberRepository members,
                            IServerOverwriteRepository overwrites)
        {
            _admin = admin;
            _filterValidator = filterValidator;
            _banValidator = banValidator;
            _servers = servers;
            _members = members;
            _overwrites = overwrites;
        }

        public async Task<AdminServerDetailDto> GetServerAsync(int serverId)
        {
            var server = await _admin.GetServerAsync(serverId) ?? throw new Helpers.Exceptions.NotFoundException("Server not found.");
            var bans = await _admin.GetBanCountsForServersAsync(new List<int> { serverId });
            var members = await _admin.GetMemberCountsForServersAsync(new List<int> { serverId });

            return new AdminServerDetailDto
            {
                Id = server.Id,
                PublicId = server.PublicId,
                Name = server.Name,
                IconUrl = server.IconUrl,
                Description = server.Description,
                InviteCode = server.InviteCode,
                OwnerUserName = server.Owner?.UserName ?? string.Empty,
                OwnerDisplayName = server.Owner?.DisplayName ?? string.Empty,
                IsPrivate = server.IsPrivate,
                CreatedAt = server.CreatedAt,
                Members = members.TryGetValue(serverId, out var memberCount) ? memberCount : 0,
                TextChannels = await _admin.CountChannelsAsync(serverId, ServerChannelKind.Text),
                VoiceChannels = await _admin.CountChannelsAsync(serverId, ServerChannelKind.Voice),
                Roles = await _admin.CountRolesAsync(serverId),
                Messages = await _admin.CountMessagesInServerAsync(serverId),
                MemberBans = bans.TryGetValue(serverId, out var banCount) ? banCount : 0,
                IsBanned = server.IsBanned,
                BannedAt = server.BannedAt,
                BanReason = server.BanReason,
                BannedByUserName = server.BannedById is null ? string.Empty : await _admin.GetUserNameAsync(server.BannedById) ?? string.Empty,
            };
        }

        public Task<AdminServerMemberListDto> GetServerMembersAsync(int serverId, AdminListFilterDto filter)
        {
            return LoadMembersAsync(serverId, null, filter);
        }

        public async Task<AdminServerMemberListDto> SearchServerMembersAsync(int serverId, AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);
            return await LoadMembersAsync(serverId, string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search, filter);
        }

        private async Task<AdminServerMemberListDto> LoadMembersAsync(int serverId, string? term, AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            var server = await _admin.GetServerAsync(serverId) ?? throw new Helpers.Exceptions.NotFoundException("Server not found.");
            var (members, total) = await _admin.GetServerMembersAsync(serverId, term, filter.Page, filter.PageSize);

            return new AdminServerMemberListDto
            {
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = members.Select(member => new AdminServerMemberDto
                {
                    UserName = member.User.UserName ?? string.Empty,
                    DisplayName = member.User.DisplayName,
                    AvatarUrl = member.User.AvatarUrl,
                    JoinedAt = member.JoinedAt,
                    IsOwner = member.UserId == server.OwnerId
                }).ToList()
            };
        }

        public async Task<List<string>> BanServerAsync(string adminId, int serverId, BanServerDto model)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_banValidator, model);

            var server = await _admin.GetServerAsync(serverId) ?? throw new Helpers.Exceptions.NotFoundException("Server not found.");
            if (server.IsBanned)
            {
                throw new Helpers.Exceptions.BadRequestException("That server is already banned.");
            }

            await _admin.SetServerBanAsync(serverId, true, model.Reason.Trim(), adminId);
            return await _members.GetUserIdsAsync(serverId);
        }

        public async Task<List<string>> UnbanServerAsync(int serverId)
        {
            var server = await _admin.GetServerAsync(serverId) ?? throw new Helpers.Exceptions.NotFoundException("Server not found.");
            if (!server.IsBanned)
            {
                throw new Helpers.Exceptions.BadRequestException("That server is not banned.");
            }

            await _admin.SetServerBanAsync(serverId, false, string.Empty, null);
            return await _members.GetUserIdsAsync(serverId);
        }

        public async Task<List<string>> DeleteServerAsync(int serverId)
        {
            var server = await _servers.GetByIdAsync(serverId) ?? throw new Helpers.Exceptions.NotFoundException("Server not found.");
            var memberIds = await _members.GetUserIdsAsync(serverId);
            await _overwrites.DeleteForServerAsync(serverId);
            await _servers.DeleteAsync(server);
            return memberIds;
        }

        public async Task<AdminUserListDto> GetUsersAsync(AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            var (users, total) = await _admin.GetUsersAsync(filter.Search, filter.Role, filter.BannedOnly, filter.Page, filter.PageSize);
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

        public async Task<AdminServerListDto> GetAllServersAsync(AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            var (servers, total) = await _admin.GetAllServersAsync(filter.BannedOnly, filter.Page, filter.PageSize);
            return await ToServerListAsync(servers, total, filter);
        }

        public async Task<AdminServerListDto> SearchServersAsync(AdminListFilterDto filter)
        {
            await Helpers.Validators.ValidationExtensions.EnsureValidAsync(_filterValidator, filter);

            if (string.IsNullOrWhiteSpace(filter.Search))
            {
                return await GetAllServersAsync(filter);
            }

            var (servers, total) = await _admin.SearchServersAsync(filter.Search, filter.BannedOnly, filter.Page, filter.PageSize);
            return await ToServerListAsync(servers, total, filter);
        }

        private async Task<AdminServerListDto> ToServerListAsync(List<Domain.Entities.Server> servers, int total, AdminListFilterDto filter)
        {
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
                    IsBanned = server.IsBanned,
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
                SuperAdmins = await _admin.CountUsersInRoleAsync(Roles.SuperAdmin)
            };

            overview.Servers = new AdminServerStatsDto
            {
                Total = await _admin.CountServersAsync(),
                Public = await _admin.CountServersByPrivacyAsync(false),
                Private = await _admin.CountServersByPrivacyAsync(true),
                Banned = await _admin.CountBannedServersAsync()
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
                Comments = await _admin.CountCommentsAsync()
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
