using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServerVoice;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerVoiceService : IServerVoiceService
    {
        private readonly IServerVoiceStateRepository _states;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IServerChannelRepository _channels;
        private readonly IServerPermissionService _permissions;
        private readonly IAccountRepository _accounts;
        private readonly IServerAuditService _audit;
        private readonly IServerVoiceRestrictionRepository _restrictions;

        public ServerVoiceService(IServerVoiceStateRepository states,
                                  IServerRepository servers,
                                  IServerMemberRepository members,
                                  IServerChannelRepository channels,
                                  IServerPermissionService permissions,
                                  IAccountRepository accounts,
                                  IServerAuditService audit,
                                  IServerVoiceRestrictionRepository restrictions)
        {
            _states = states;
            _servers = servers;
            _members = members;
            _channels = channels;
            _permissions = permissions;
            _accounts = accounts;
            _audit = audit;
            _restrictions = restrictions;
        }

        public async Task<ServerVoiceDto> GetAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);

            var memberIds = (await _members.GetUserIdsAsync(serverId)).ToHashSet();
            var result = new ServerVoiceDto { CanModerate = await CanModerateAsync(userId, serverId) };
            var restrictions = (await _restrictions.GetForServerAsync(serverId)).ToDictionary(x => x.UserId);
            var visible = new Dictionary<int, bool>();
            var people = new Dictionary<string, ServerVoiceUserDto>();
            foreach (var state in await _states.GetForServerAsync(serverId))
            {
                if (!memberIds.Contains(state.UserId))
                {
                    await _states.DeleteForUserInServerAsync(serverId, state.UserId);
                    continue;
                }

                if (!visible.TryGetValue(state.ChannelId, out var canSee))
                {
                    var channel = await _channels.GetInServerAsync(serverId, state.ChannelId);
                    canSee = channel is not null && await _permissions.CanViewChannelAsync(userId, serverId, channel);
                    visible[state.ChannelId] = canSee;
                }

                if (!canSee)
                {
                    continue;
                }

                if (!people.TryGetValue(state.UserId, out var user))
                {
                    var account = await _accounts.GetByIdAsync(state.UserId);
                    user = new ServerVoiceUserDto
                    {
                        UserId = state.UserId,
                        UserName = account?.UserName ?? string.Empty,
                        DisplayName = account is null ? string.Empty : string.IsNullOrWhiteSpace(account.DisplayName) ? account.UserName ?? string.Empty : account.DisplayName,
                        AvatarUrl = account?.AvatarUrl ?? string.Empty
                    };
                    people[state.UserId] = user;
                }

                var group = result.Channels.FirstOrDefault(c => c.ChannelId == state.ChannelId);
                if (group is null)
                {
                    group = new ServerVoiceChannelDto { ChannelId = state.ChannelId, StartedAt = DateTime.SpecifyKind(state.SessionStartedAt, DateTimeKind.Utc) };
                    result.Channels.Add(group);
                }

                var started = DateTime.SpecifyKind(state.SessionStartedAt, DateTimeKind.Utc);
                if (started < group.StartedAt)
                {
                    group.StartedAt = started;
                }

                restrictions.TryGetValue(state.UserId, out var restriction);
                var serverMuted = restriction?.Muted == true;
                var serverDeafened = restriction?.Deafened == true;
                group.Users.Add(new ServerVoiceUserDto
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl,
                    IsMuted = state.IsMuted || state.IsDeafened || serverMuted || serverDeafened,
                    IsDeafened = state.IsDeafened || serverDeafened,
                    IsServerMuted = serverMuted,
                    IsServerDeafened = serverDeafened,
                    IsCameraOn = state.IsCameraOn,
                    IsStreaming = state.IsStreaming,
                    IsSelf = state.UserId == userId
                });
            }

            result.Current = await CurrentAsync(userId);
            return result;
        }

        public async Task<ServerVoiceChangeDto> JoinAsync(string userId, int serverId, int channelId, JoinServerVoiceDto model)
        {
            await EnsureMemberAsync(userId, serverId);

            var channel = await _channels.GetInServerAsync(serverId, channelId);
            if (channel is null)
            {
                throw new NotFoundException("Channel not found.");
            }

            if (channel.Kind != ServerChannelKind.Voice)
            {
                throw new BadRequestException("That is not a voice channel.");
            }

            var membership = await _members.GetAsync(serverId, userId);
            if (membership?.TimeoutUntil is not null && membership.TimeoutUntil > DateTime.UtcNow)
            {
                throw new ForbiddenException("You are timed out and can't join voice channels right now.");
            }

            var perms = await _permissions.GetInChannelAsync(userId, serverId, channel);
            if ((perms & ServerPermission.ViewChannels) != ServerPermission.ViewChannels
                || (perms & ServerPermission.Connect) != ServerPermission.Connect)
            {
                throw new ForbiddenException("You do not have permission to connect to this voice channel.");
            }

            var change = new ServerVoiceChangeDto();
            var restriction = await _restrictions.GetAsync(serverId, userId);
            if (restriction is not null && (restriction.Muted || restriction.Deafened))
            {
                // a moderator's mute / deafen follows the person into every voice channel of the server, however often they leave and come back
                model.IsMuted = model.IsMuted || restriction.Muted || restriction.Deafened;
                model.IsDeafened = model.IsDeafened || restriction.Deafened;
            }

            var state = await _states.GetForUserAsync(userId);
            var others = (await _states.GetForServerAsync(serverId)).Where(x => x.ChannelId == channelId && x.UserId != userId).ToList();
            var startedAt = others.Count > 0 ? others.Min(x => x.SessionStartedAt) : DateTime.UtcNow;
            if (channel.UserLimit > 0 && (state is null || state.ChannelId != channelId)
                && (perms & ServerPermission.ManageChannels) != ServerPermission.ManageChannels)
            {
                var inside = others.Count;
                if (inside >= channel.UserLimit)
                {
                    throw new BadRequestException("This voice channel is full.");
                }
            }

            if (state is null)
            {
                await _states.AddAsync(new ServerVoiceState
                {
                    ServerId = serverId,
                    ChannelId = channelId,
                    UserId = userId,
                    IsMuted = model.IsMuted,
                    IsDeafened = model.IsDeafened,
                    SessionStartedAt = startedAt
                });
            }
            else
            {
                if (state.ServerId == serverId && state.ChannelId == channelId)
                {
                    return change;
                }

                if (state.ServerId != serverId)
                {
                    change.ServerIds.Add(state.ServerId);
                }

                state.ServerId = serverId;
                state.ChannelId = channelId;
                state.CreatedAt = DateTime.UtcNow;
                state.SessionStartedAt = startedAt;
                await _states.UpdateAsync(state);
            }

            change.ServerIds.Add(serverId);
            return change;
        }

        public async Task<ServerVoiceChangeDto> LeaveAsync(string userId)
        {
            var change = new ServerVoiceChangeDto();
            var state = await _states.GetForUserAsync(userId);
            if (state is not null)
            {
                change.ServerIds.Add(state.ServerId);
                await _states.DeleteAsync(state);
            }

            return change;
        }

        public async Task<ServerVoiceChangeDto> UpdateAsync(string userId, UpdateServerVoiceDto model)
        {
            var change = new ServerVoiceChangeDto();
            var state = await _states.GetForUserAsync(userId);
            if (state is null)
            {
                return change;
            }

            if (state.IsMuted != model.IsMuted || state.IsDeafened != model.IsDeafened || state.IsCameraOn != model.IsCameraOn || state.IsStreaming != model.IsStreaming)
            {
                state.IsMuted = model.IsMuted;
                state.IsDeafened = model.IsDeafened;
                state.IsCameraOn = model.IsCameraOn;
                state.IsStreaming = model.IsStreaming;
                await _states.UpdateAsync(state);
                change.ServerIds.Add(state.ServerId);
            }

            return change;
        }

        public async Task<ServerVoiceModerationDto> ModerateAsync(string actorId, int serverId, string targetUserId, ModerateServerVoiceDto model)
        {
            // a moderator may also server-mute themselves; for anybody else the role order applies
            var (state, name) = await LoadTargetAsync(actorId, serverId, targetUserId, "change the voice of", allowSelf: true);

            var found = await _restrictions.GetAsync(serverId, targetUserId);
            var restriction = found ?? new ServerVoiceRestriction { ServerId = serverId, UserId = targetUserId };
            var selfLift = targetUserId == actorId && found is not null && found.ByUserId != actorId;

            var kind = string.Empty;
            if (model.ServerMuted is not null && model.ServerMuted != restriction.Muted)
            {
                if (selfLift && !model.ServerMuted.Value)
                {
                    throw new ForbiddenException("Someone else muted you here. Only they or a higher role can lift it.");
                }

                restriction.Muted = model.ServerMuted.Value;
                kind = restriction.Muted ? "muted" : "unmuted";
                await _audit.LogAsync(serverId, actorId, restriction.Muted ? ServerAuditAction.MemberMute : ServerAuditAction.MemberUnmute, "Member", targetUserId, name,
                    restriction.Muted ? "Server muted in voice" : "Removed the server mute");
            }

            if (model.ServerDeafened is not null && model.ServerDeafened != restriction.Deafened)
            {
                if (selfLift && !model.ServerDeafened.Value)
                {
                    throw new ForbiddenException("Someone else deafened you here. Only they or a higher role can lift it.");
                }

                restriction.Deafened = model.ServerDeafened.Value;
                kind = restriction.Deafened ? "deafened" : "undeafened";
                await _audit.LogAsync(serverId, actorId, restriction.Deafened ? ServerAuditAction.MemberMute : ServerAuditAction.MemberUnmute, "Member", targetUserId, name,
                    restriction.Deafened ? "Server deafened in voice" : "Removed the server deafen");
            }

            var result = new ServerVoiceModerationDto { TargetUserId = targetUserId, Kind = kind };
            if (kind.Length == 0)
            {
                return result;
            }

            if (restriction.Muted || restriction.Deafened)
            {
                restriction.ByUserId = actorId;
                if (found is null)
                {
                    await _restrictions.AddAsync(restriction);
                }
                else
                {
                    await _restrictions.UpdateAsync(restriction);
                }
            }
            else if (found is not null)
            {
                await _restrictions.DeleteAsync(found);
            }

            // the room list reads the effective state, so a server deafen also marks the stored state
            if (restriction.Deafened && !state.IsDeafened)
            {
                state.IsDeafened = true;
                await _states.UpdateAsync(state);
            }

            result.Change.ServerIds.Add(serverId);
            return result;
        }

        public async Task<ServerVoiceModerationDto> DisconnectAsync(string actorId, int serverId, string targetUserId)
        {
            var (state, name) = await LoadTargetAsync(actorId, serverId, targetUserId, "disconnect", allowSelf: false);

            await _states.DeleteAsync(state);
            await _audit.LogAsync(serverId, actorId, ServerAuditAction.MemberVoiceDisconnect, "Member", targetUserId, name, "Disconnected from voice");

            var result = new ServerVoiceModerationDto { TargetUserId = targetUserId, Kind = "disconnected" };
            result.Change.ServerIds.Add(serverId);
            return result;
        }

        public async Task<bool> AreTogetherAsync(string userId, string otherUserId)
        {
            var mine = await _states.GetForUserAsync(userId);
            var theirs = await _states.GetForUserAsync(otherUserId);
            return mine is not null && theirs is not null && mine.ChannelId == theirs.ChannelId;
        }

        public async Task<List<string>> GetMemberIdsAsync(int serverId)
        {
            return await _members.GetUserIdsAsync(serverId);
        }

        private async Task<ServerVoiceCurrentDto?> CurrentAsync(string userId)
        {
            var state = await _states.GetForUserAsync(userId);
            if (state is null)
            {
                return null;
            }

            var channel = await _channels.GetInServerAsync(state.ServerId, state.ChannelId);
            var server = await _servers.GetByIdAsync(state.ServerId);
            if (channel is null || server is null || await _members.GetAsync(state.ServerId, userId) is null)
            {
                await _states.DeleteAsync(state);
                return null;
            }

            var mine = await _restrictions.GetAsync(state.ServerId, userId);
            var serverMuted = mine?.Muted == true;
            var serverDeafened = mine?.Deafened == true;
            return new ServerVoiceCurrentDto
            {
                ServerId = server.Id,
                ServerName = server.Name,
                ChannelId = channel.Id,
                ChannelName = channel.Name,
                JoinedAt = DateTime.SpecifyKind(state.CreatedAt, DateTimeKind.Utc),
                IsMuted = state.IsMuted || state.IsDeafened || serverMuted || serverDeafened,
                IsDeafened = state.IsDeafened || serverDeafened,
                IsServerMuted = serverMuted,
                IsServerDeafened = serverDeafened,
                IsCameraOn = state.IsCameraOn,
                IsStreaming = state.IsStreaming
            };
        }

        private async Task<bool> CanModerateAsync(string userId, int serverId)
        {
            return (await _permissions.GetAsync(userId, serverId) & ServerPermission.MuteMembers) == ServerPermission.MuteMembers;
        }

        // the actor needs Mute Members; for another person a higher role too, and the target must be in voice on this server
        private async Task<(ServerVoiceState State, string Name)> LoadTargetAsync(string actorId, int serverId, string targetUserId, string verb, bool allowSelf)
        {
            await EnsureMemberAsync(actorId, serverId);
            await _permissions.EnsureAsync(actorId, serverId, ServerPermission.MuteMembers);

            if (targetUserId == actorId)
            {
                if (!allowSelf)
                {
                    throw new BadRequestException("Use your own microphone and headphone buttons for that.");
                }
            }
            else
            {
                await _permissions.EnsureCanActOnAsync(actorId, serverId, targetUserId, verb);
            }

            var state = await _states.GetForUserAsync(targetUserId);
            if (await _members.GetAsync(serverId, targetUserId) is null || state is null || state.ServerId != serverId)
            {
                throw new NotFoundException("That member is not in a voice channel of this server.");
            }

            var account = await _accounts.GetByIdAsync(targetUserId);
            return (state, account?.UserName ?? targetUserId);
        }

        private async Task EnsureMemberAsync(string userId, int serverId)
        {
            if (await _members.GetAsync(serverId, userId) is null)
            {
                throw new NotFoundException("Server not found.");
            }
        }
    }
}
