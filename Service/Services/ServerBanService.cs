using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerBans;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerBanService : IServerBanService
    {
        private readonly IServerBanRepository _bans;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IAccountRepository _accounts;
        private readonly IServerPermissionService _permissions;
        private readonly IServerAuditService _audit;
        private readonly IValidator<BanServerMemberDto> _banValidator;
        private readonly IValidator<ServerBanFilterDto> _filterValidator;

        public ServerBanService(IServerBanRepository bans,
                                IServerRepository servers,
                                IServerMemberRepository members,
                                IAccountRepository accounts,
                                IServerPermissionService permissions,
                                IServerAuditService audit,
                                IValidator<BanServerMemberDto> banValidator,
                                IValidator<ServerBanFilterDto> filterValidator)
        {
            _bans = bans;
            _servers = servers;
            _members = members;
            _accounts = accounts;
            _permissions = permissions;
            _audit = audit;
            _banValidator = banValidator;
            _filterValidator = filterValidator;
        }

        public async Task<IEnumerable<ServerBanDto>> GetBansAsync(string userId, int serverId, ServerBanFilterDto filter)
        {
            await _filterValidator.EnsureValidAsync(filter);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.BanMembers);

            var bans = await _bans.GetForServerAsync(serverId, filter.Search, filter.BeforeId, filter.Take);
            return bans.Select(ToDto).ToList();
        }

        public async Task<ServerBanDto> BanAsync(string userId, int serverId, BanServerMemberDto model)
        {
            await _banValidator.EnsureValidAsync(model);
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.BanMembers);

            var server = await GetServerAsync(serverId);
            var target = await GetTargetAsync(userId, server, model.UserId, "ban");
            if (await _bans.IsBannedAsync(serverId, target.Id))
            {
                throw new BadRequestException("That person is already banned.");
            }

            await EnsureCanActOnAsync(userId, server, target.Id, "ban");

            var membership = await _members.GetAsync(serverId, target.Id);
            if (membership is not null)
            {
                await _members.DeleteAsync(membership);
            }

            var ban = new ServerBan
            {
                ServerId = serverId,
                UserId = target.Id,
                BannedById = userId,
                Reason = model.Reason?.Trim() ?? string.Empty
            };
            await _bans.AddAsync(ban);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberBan, "Member", target.Id, DisplayOf(target), string.IsNullOrEmpty(ban.Reason) ? "Banned from the server" : "Banned from the server. Reason: " + ban.Reason);

            var actor = await _accounts.GetByIdAsync(userId);
            return new ServerBanDto
            {
                Id = ban.Id,
                UserId = target.Id,
                UserName = target.UserName ?? string.Empty,
                DisplayName = DisplayOf(target),
                AvatarUrl = target.AvatarUrl,
                Reason = ban.Reason,
                BannedByUserName = actor?.UserName ?? string.Empty,
                CreatedAt = DateTime.SpecifyKind(ban.CreatedAt, DateTimeKind.Utc)
            };
        }

        public async Task UnbanAsync(string userId, int serverId, string targetUserId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.BanMembers);

            var ban = await _bans.GetAsync(serverId, targetUserId);
            if (ban is null)
            {
                throw new NotFoundException("That person is not banned.");
            }

            await _bans.DeleteAsync(ban);
            var target = await _accounts.GetByIdAsync(targetUserId);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberUnban, "Member", targetUserId, target is null ? string.Empty : DisplayOf(target), "Removed the ban");
        }

        public async Task KickAsync(string userId, int serverId, string targetUserId)
        {
            await _permissions.EnsureAsync(userId, serverId, ServerPermission.KickMembers);

            var server = await GetServerAsync(serverId);
            var target = await GetTargetAsync(userId, server, targetUserId, "kick");
            var membership = await _members.GetAsync(serverId, target.Id);
            if (membership is null)
            {
                throw new NotFoundException("That person is not in this server.");
            }

            await EnsureCanActOnAsync(userId, server, target.Id, "kick");
            await _members.DeleteAsync(membership);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberKick, "Member", target.Id, DisplayOf(target), "Kicked from the server");
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
        }

        private async Task<Server> GetServerAsync(int serverId)
        {
            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                throw new NotFoundException("Server not found.");
            }

            return server;
        }

        private async Task<AppUser> GetTargetAsync(string userId, Server server, string targetUserId, string verb)
        {
            var target = await _accounts.GetByIdAsync(targetUserId);
            if (target is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (target.Id == userId)
            {
                throw new BadRequestException("You can't " + verb + " yourself.");
            }

            if (target.Id == server.OwnerId)
            {
                throw new BadRequestException("The server owner can't be " + verb + (verb == "ban" ? "ned." : "ed."));
            }

            return target;
        }

        private async Task EnsureCanActOnAsync(string userId, Server server, string targetId, string verb)
        {
            await _permissions.EnsureCanActOnAsync(userId, server.Id, targetId, verb);
        }

        private static string DisplayOf(AppUser user)
        {
            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName;
        }

        private static ServerBanDto ToDto(ServerBan ban)
        {
            return new ServerBanDto
            {
                Id = ban.Id,
                UserId = ban.UserId,
                UserName = ban.User.UserName ?? string.Empty,
                DisplayName = DisplayOf(ban.User),
                AvatarUrl = ban.User.AvatarUrl,
                Reason = ban.Reason,
                BannedByUserName = ban.BannedBy.UserName ?? string.Empty,
                CreatedAt = DateTime.SpecifyKind(ban.CreatedAt, DateTimeKind.Utc)
            };
        }
    }
}
