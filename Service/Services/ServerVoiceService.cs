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

        public ServerVoiceService(IServerVoiceStateRepository states,
                                  IServerRepository servers,
                                  IServerMemberRepository members,
                                  IServerChannelRepository channels,
                                  IServerPermissionService permissions,
                                  IAccountRepository accounts)
        {
            _states = states;
            _servers = servers;
            _members = members;
            _channels = channels;
            _permissions = permissions;
            _accounts = accounts;
        }

        public async Task<ServerVoiceDto> GetAsync(string userId, int serverId)
        {
            await EnsureMemberAsync(userId, serverId);

            var memberIds = (await _members.GetUserIdsAsync(serverId)).ToHashSet();
            var result = new ServerVoiceDto();
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

                group.Users.Add(new ServerVoiceUserDto
                {
                    UserId = user.UserId,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl,
                    IsMuted = state.IsMuted || state.IsDeafened,
                    IsDeafened = state.IsDeafened,
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

            var perms = await _permissions.GetInChannelAsync(userId, serverId, channel);
            if ((perms & ServerPermission.ViewChannels) != ServerPermission.ViewChannels
                || (perms & ServerPermission.Connect) != ServerPermission.Connect)
            {
                throw new ForbiddenException("You do not have permission to connect to this voice channel.");
            }

            var change = new ServerVoiceChangeDto();
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

            return new ServerVoiceCurrentDto
            {
                ServerId = server.Id,
                ServerName = server.Name,
                ChannelId = channel.Id,
                ChannelName = channel.Name,
                JoinedAt = DateTime.SpecifyKind(state.CreatedAt, DateTimeKind.Utc),
                IsMuted = state.IsMuted || state.IsDeafened,
                IsDeafened = state.IsDeafened,
                IsCameraOn = state.IsCameraOn,
                IsStreaming = state.IsStreaming
            };
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
