using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;
using System.Security.Cryptography;

namespace Service.Services
{
    public class ServerService : IServerService
    {
        private const string Letters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Digits = "23456789";
        private const int CodeLength = 7;

        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IAccountRepository _accounts;
        private readonly IServerMemberRoleRepository _memberRoles;
        private readonly IServerPermissionService _permissions;
        private readonly IServerAuditService _audit;
        private readonly IServerBanRepository _bans;
        private readonly IServerChannelService _channelService;
        private readonly IValidator<DeleteServerDto> _deleteValidator;
        private readonly IServerOverwriteRepository _overwrites;
        private readonly IPresenceService _presence;
        private readonly IValidator<CreateServerDto> _createValidator;
        private readonly IValidator<JoinServerDto> _joinValidator;
        private readonly IValidator<UpdateServerPreferencesDto> _preferencesValidator;
        private readonly IValidator<ReorderServersDto> _reorderValidator;
        private readonly IValidator<UpdateServerProfileDto> _profileValidator;
        private readonly IValidator<UpdateServerBannerDto> _bannerValidator;
        private readonly IValidator<UpdateServerInviteDto> _inviteValidator;
        private readonly IMapper _mapper;

        public ServerService(IServerRepository servers,
                             IServerMemberRepository members,
                             IAccountRepository accounts,
                             IServerMemberRoleRepository memberRoles,
                             IServerPermissionService permissions,
                             IServerAuditService audit,
                             IServerBanRepository bans,
                             IServerChannelService channelService,
                             IServerOverwriteRepository overwrites,
                             IValidator<DeleteServerDto> deleteValidator,
                             IPresenceService presence,
                             IValidator<CreateServerDto> createValidator,
                             IValidator<JoinServerDto> joinValidator,
                             IValidator<UpdateServerPreferencesDto> preferencesValidator,
                             IValidator<ReorderServersDto> reorderValidator,
                             IValidator<UpdateServerProfileDto> profileValidator,
                             IValidator<UpdateServerBannerDto> bannerValidator,
                             IValidator<UpdateServerInviteDto> inviteValidator,
                             IMapper mapper)
        {
            _servers = servers;
            _members = members;
            _accounts = accounts;
            _memberRoles = memberRoles;
            _permissions = permissions;
            _audit = audit;
            _bans = bans;
            _channelService = channelService;
            _deleteValidator = deleteValidator;
            _overwrites = overwrites;
            _presence = presence;
            _createValidator = createValidator;
            _joinValidator = joinValidator;
            _preferencesValidator = preferencesValidator;
            _reorderValidator = reorderValidator;
            _profileValidator = profileValidator;
            _bannerValidator = bannerValidator;
            _inviteValidator = inviteValidator;
            _mapper = mapper;
        }

        public async Task<ServerChangeDto> CreateAsync(string userId, CreateServerDto model)
        {
            await _createValidator.EnsureValidAsync(model);

            if (await _accounts.GetByIdAsync(userId) is null)
            {
                throw new NotFoundException("User not found.");
            }

            var server = new Server
            {
                Name = model.Name.Trim(),
                OwnerId = userId,
                InviteCode = await NewInviteCodeAsync(),
                Members = new List<ServerMember>
                {
                    new ServerMember
                    {
                        UserId = userId,
                        JoinedAt = DateTime.UtcNow,
                        Position = await _members.NextPositionAsync(userId),
                        NotificationLevel = ServerNotificationLevel.All
                    }
                },
                Roles = new List<ServerRole>
                {
                    new ServerRole
                    {
                        Name = "@everyone",
                        Color = "gray",
                        Permissions = (long)ServerPermissionService.EveryoneDefault,
                        Position = 0,
                        IsEveryone = true
                    }
                }
            };
            await _servers.AddAsync(server);
            await _channelService.EnsureDefaultsAsync(server.Id);
            await _audit.LogAsync(server.Id, userId, ServerAuditAction.ServerUpdate, "Server", server.Id.ToString(), server.Name, "Created the server");

            var fresh = (await _servers.GetWithMembersAsync(server.Id))!;
            return Change(fresh, userId, true);
        }

        public async Task<ServerChangeDto> JoinAsync(string userId, JoinServerDto model)
        {
            await _joinValidator.EnsureValidAsync(model);

            var server = await _servers.GetByInviteCodeAsync(ExtractCode(model.Invite));
            if (server is null)
            {
                throw new NotFoundException("That invite is invalid or has expired.");
            }

            if (server.Members.Any(m => m.UserId == userId))
            {
                return Change(server, userId, false);
            }

            if (await _bans.IsBannedAsync(server.Id, userId))
            {
                throw new BadRequestException("You are banned from this server.");
            }

            await _members.AddAsync(new ServerMember
            {
                ServerId = server.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                Position = await _members.NextPositionAsync(userId),
                JoinMethod = server.InviteCode,
                NotificationLevel = ServerNotificationLevel.Mentions
            });
            var joiner = await _accounts.GetByIdAsync(userId);
            await _audit.LogAsync(server.Id, userId, ServerAuditAction.MemberJoin, "Member", userId, joiner?.DisplayName ?? string.Empty, "Joined using the invite link " + server.InviteCode);

            var fresh = (await _servers.GetWithMembersAsync(server.Id))!;
            return Change(fresh, userId, true);
        }

        public async Task<IEnumerable<ServerDto>> GetMineAsync(string userId)
        {
            var servers = await _servers.GetForUserAsync(userId);
            return servers.Select(server => ToDto(server, userId)).ToList();
        }

        public async Task<ServerDto> GetAsync(string userId, int serverId)
        {
            var server = await _servers.GetWithMembersAsync(serverId);
            if (server is null || server.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Server not found.");
            }

            var dto = ToDto(server, userId);
            dto.Members = (await BuildMembersAsync(server, userId)).OrderBy(m => m.JoinedAt).ToList();
            dto.Permissions = (long)await _permissions.GetAsync(userId, serverId);
            dto.OnlineCount = dto.Members.Count(m => m.Status != "offline");
            return dto;
        }

        public async Task<IEnumerable<ServerMemberDto>> GetMembersAsync(string userId, int serverId)
        {
            var server = await _servers.GetWithMembersAsync(serverId);
            if (server is null || server.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Server not found.");
            }

            return (await BuildMembersAsync(server, userId)).OrderByDescending(m => m.JoinedAt).ToList();
        }

        private async Task<List<ServerMemberDto>> BuildMembersAsync(Server server, string viewerId)
        {
            var members = new List<ServerMemberDto>();
            var roleLinks = (await _memberRoles.GetForServerAsync(server.Id))
                .GroupBy(link => link.ServerMember.UserId)
                .ToDictionary(group => group.Key, group => group.Select(link => link.Role).OrderByDescending(role => role.Position).ToList());
            var viewerTop = await _permissions.GetTopPositionAsync(viewerId, server.Id);
            foreach (var member in server.Members)
            {
                var memberTop = member.UserId == server.OwnerId ? int.MaxValue : roleLinks.TryGetValue(member.UserId, out var tops) && tops.Count > 0 ? tops.Max(r => r.Position) : 0;
                members.Add(new ServerMemberDto
                {
                    CanModerate = member.UserId != viewerId && member.UserId != server.OwnerId && viewerTop > memberTop,
                    TimeoutUntil = member.TimeoutUntil is not null && member.TimeoutUntil > DateTime.UtcNow ? DateTime.SpecifyKind(member.TimeoutUntil.Value, DateTimeKind.Utc) : null,
                    Id = member.UserId,
                    UserName = member.User.UserName ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(member.User.DisplayName) ? member.User.UserName ?? string.Empty : member.User.DisplayName,
                    AvatarUrl = member.User.AvatarUrl,
                    CustomStatus = member.User.CustomStatus,
                    Status = await _presence.ResolveAsync(member.User),
                    IsOwner = member.UserId == server.OwnerId,
                    IsSelf = member.UserId == viewerId,
                    JoinedAt = DateTime.SpecifyKind(member.JoinedAt, DateTimeKind.Utc),
                    AccountCreatedAt = DateTime.SpecifyKind(member.User.CreatedAt, DateTimeKind.Utc),
                    JoinMethod = member.JoinMethod,
                    Roles = roleLinks.TryGetValue(member.UserId, out var held) ? _mapper.Map<List<ServerRoleRefDto>>(held) : new List<ServerRoleRefDto>()
                });
            }

            return members;
        }

        public async Task<ServerChangeDto> UpdateProfileAsync(string userId, int serverId, UpdateServerProfileDto model)
        {
            await _profileValidator.EnsureValidAsync(model);

            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                throw new NotFoundException("Server not found.");
            }

            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);

            var changes = new List<string>();
            var name = model.Name.Trim();
            var description = model.Description?.Trim() ?? string.Empty;
            var banner = model.BannerColor.Trim().ToLowerInvariant();
            if (server.Name != name)
            {
                changes.Add("Changed the server name from \"" + server.Name + "\" to \"" + name + "\"");
            }

            if (server.Description != description)
            {
                changes.Add("Changed the server description");
            }

            if (server.BannerColor != banner)
            {
                changes.Add("Changed the banner color from " + server.BannerColor + " to " + banner);
            }

            if (server.IsPrivate != model.IsPrivate)
            {
                changes.Add(model.IsPrivate ? "Made the server profile private" : "Made the server profile public");
            }

            if (model.IconUrl is not null && server.IconUrl != model.IconUrl.Trim())
            {
                changes.Add("Changed the server icon");
            }

            server.Name = name;
            server.Description = description;
            server.BannerColor = banner;
            server.IsPrivate = model.IsPrivate;
            if (model.IconUrl is not null)
            {
                server.IconUrl = model.IconUrl.Trim();
            }

            await _servers.UpdateAsync(server);
            if (changes.Count > 0)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.ServerUpdate, "Server", serverId.ToString(), server.Name, string.Join("; ", changes));
            }

            var fresh = (await _servers.GetWithMembersAsync(serverId))!;
            return Change(fresh, userId, false);
        }

        public async Task<ServerChangeDto> UpdateBannerAsync(string userId, int serverId, UpdateServerBannerDto model)
        {
            await _bannerValidator.EnsureValidAsync(model);

            var server = await GetOwnedAsync(userId, serverId);
            var bannerUrl = model.BannerUrl.Trim();
            var bannerChanged = server.BannerUrl != bannerUrl;
            var bannerRemoved = string.IsNullOrEmpty(bannerUrl);
            server.BannerUrl = bannerUrl;
            await _servers.UpdateAsync(server);
            if (bannerChanged)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.ServerUpdate, "Server", serverId.ToString(), server.Name, bannerRemoved ? "Removed the server banner image" : "Changed the server banner image");
            }

            return Change((await _servers.GetWithMembersAsync(serverId))!, userId, false);
        }

        public async Task<ServerChangeDto> UpdateInviteAsync(string userId, int serverId, UpdateServerInviteDto model)
        {
            await _inviteValidator.EnsureValidAsync(model);

            var server = await GetOwnedAsync(userId, serverId);
            var code = model.InviteCode.Trim();
            var taken = await _servers.GetByInviteCodeAsync(code);
            if (taken is not null && taken.Id != serverId)
            {
                throw new BadRequestException("That link is already taken.");
            }

            var oldCode = server.InviteCode;
            server.InviteCode = code;
            await _servers.UpdateAsync(server);
            if (oldCode != code)
            {
                await _audit.LogAsync(serverId, userId, ServerAuditAction.InviteUpdate, "Server", serverId.ToString(), server.Name, "Changed the invite link from " + oldCode + " to " + code);
            }

            return Change((await _servers.GetWithMembersAsync(serverId))!, userId, false);
        }

        private async Task<Server> GetOwnedAsync(string userId, int serverId)
        {
            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                throw new NotFoundException("Server not found.");
            }

            await _permissions.EnsureAsync(userId, serverId, ServerPermission.ManageServer);
            return server;
        }

        public async Task<List<string>> DeleteAsync(string userId, int serverId, DeleteServerDto model)
        {
            await _deleteValidator.EnsureValidAsync(model);

            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                throw new NotFoundException("Server not found.");
            }

            if (server.OwnerId != userId)
            {
                throw new ForbiddenException("Only the server owner can delete the server.");
            }

            if (model.Name.Trim() != server.Name)
            {
                throw new BadRequestException("The name you typed doesn't match the server name.");
            }

            var memberIds = await _members.GetUserIdsAsync(serverId);
            await _overwrites.DeleteForServerAsync(serverId);
            await _servers.DeleteAsync(server);
            return memberIds;
        }

        public async Task<ServerChangeDto> LeaveAsync(string userId, int serverId)
        {
            var server = await _servers.GetWithMembersAsync(serverId);
            var membership = server?.Members.FirstOrDefault(m => m.UserId == userId);
            if (server is null || membership is null)
            {
                throw new NotFoundException("Server not found.");
            }

            if (server.OwnerId == userId)
            {
                throw new BadRequestException("The owner cannot leave the server.");
            }

            await _members.DeleteAsync(membership);
            var leaver = await _accounts.GetByIdAsync(userId);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.MemberLeave, "Member", userId, leaver?.DisplayName ?? string.Empty, "Left the server");

            var fresh = (await _servers.GetWithMembersAsync(serverId))!;
            var change = Change(fresh, userId, true);
            change.MemberIds = fresh.Members.Select(m => m.UserId).ToList();
            return change;
        }

        public async Task<ServerInviteDto> GetInviteAsync(string userId, string invite)
        {
            var server = await _servers.GetByInviteCodeAsync(ExtractCode(invite ?? string.Empty));
            if (server is null)
            {
                throw new NotFoundException("That invite is invalid or has expired.");
            }

            if (await _bans.IsBannedAsync(server.Id, userId))
            {
                return new ServerInviteDto { ServerId = server.Id, InviteCode = server.InviteCode, IsBanned = true };
            }

            var online = 0;
            foreach (var member in server.Members)
            {
                if (await _presence.ResolveAsync(member.User) != "offline")
                {
                    online++;
                }
            }

            return new ServerInviteDto
            {
                ServerId = server.Id,
                Name = server.Name,
                IconUrl = server.IconUrl,
                InviteCode = server.InviteCode,
                Description = server.Description,
                BannerUrl = server.BannerUrl,
                BannerColor = server.BannerColor,
                IsPrivate = server.IsPrivate,
                MemberCount = server.Members.Count,
                OnlineCount = online,
                IsMember = server.Members.Any(m => m.UserId == userId),
                CreatedAt = server.CreatedAt
            };
        }

        public async Task<ServerDto> UpdatePreferencesAsync(string userId, int serverId, UpdateServerPreferencesDto model)
        {
            await _preferencesValidator.EnsureValidAsync(model);

            var membership = await _members.GetAsync(serverId, userId);
            if (membership is null)
            {
                throw new NotFoundException("Server not found.");
            }

            if (model.MuteMinutes is not null)
            {
                membership.MutedIndefinitely = model.MuteMinutes == -1;
                membership.MutedUntil = model.MuteMinutes > 0 ? DateTime.UtcNow.AddMinutes(model.MuteMinutes.Value) : null;
            }

            if (model.NotificationLevel is not null)
            {
                membership.NotificationLevel = model.NotificationLevel.Trim().ToLowerInvariant() switch
                {
                    "all" => ServerNotificationLevel.All,
                    "nothing" => ServerNotificationLevel.Nothing,
                    _ => ServerNotificationLevel.Mentions
                };
            }

            if (model.HideMutedChannels is not null)
            {
                membership.HideMutedChannels = model.HideMutedChannels.Value;
            }

            if (model.ShowAllChannels is not null)
            {
                membership.ShowAllChannels = model.ShowAllChannels.Value;
            }

            await _members.UpdateAsync(membership);
            return await GetAsync(userId, serverId);
        }

        public async Task ReorderAsync(string userId, ReorderServersDto model)
        {
            await _reorderValidator.EnsureValidAsync(model);

            var memberships = (await _members.GetForUserAsync(userId)).ToList();
            var order = model.ServerIds.Distinct().Where(id => memberships.Any(m => m.ServerId == id)).ToList();
            var rest = memberships.Where(m => !order.Contains(m.ServerId)).OrderBy(m => m.Position).Select(m => m.ServerId);
            var final = order.Concat(rest).ToList();
            for (var index = 0; index < final.Count; index++)
            {
                var membership = memberships.First(m => m.ServerId == final[index]);
                if (membership.Position == index)
                {
                    continue;
                }

                membership.Position = index;
                await _members.UpdateAsync(membership);
            }
        }

        private ServerDto ToDto(Server server, string viewerId)
        {
            var dto = _mapper.Map<ServerDto>(server, ForViewer(viewerId));
            var membership = server.Members.FirstOrDefault(m => m.UserId == viewerId);
            if (membership is not null)
            {
                dto.Position = membership.Position;
                dto.Muted = membership.MutedIndefinitely || (membership.MutedUntil.HasValue && membership.MutedUntil.Value > DateTime.UtcNow);
                dto.NotificationLevel = membership.NotificationLevel.ToString().ToLowerInvariant();
                dto.HideMutedChannels = membership.HideMutedChannels;
                dto.ShowAllChannels = membership.ShowAllChannels;
            }

            return dto;
        }

        private ServerChangeDto Change(Server server, string viewerId, bool joined)
        {
            return new ServerChangeDto
            {
                Server = ToDto(server, viewerId),
                MemberIds = server.Members.Select(m => m.UserId).ToList(),
                Joined = joined
            };
        }

        private static string ExtractCode(string invite)
        {
            var text = invite.Trim();
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
            {
                text = uri.AbsolutePath;
            }

            var query = text.IndexOfAny(new[] { '?', '#' });
            if (query >= 0)
            {
                text = text.Substring(0, query);
            }

            return text.TrimEnd('/').Split('/').Last().Trim();
        }

        private async Task<string> NewInviteCodeAsync()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = RandomCode();
                if (!await _servers.InviteCodeExistsAsync(code))
                {
                    return code;
                }
            }

            throw new BadRequestException("Could not create an invite link. Try again.");
        }

        private static string RandomCode()
        {
            var all = Letters + Digits;
            var chars = new char[CodeLength];
            for (var i = 0; i < CodeLength; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            var digitAt = RandomNumberGenerator.GetInt32(CodeLength);
            var letterAt = (digitAt + 1 + RandomNumberGenerator.GetInt32(CodeLength - 1)) % CodeLength;
            chars[digitAt] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            chars[letterAt] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
            return new string(chars);
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
