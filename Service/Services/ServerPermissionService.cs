using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServerPermissionService : IServerPermissionService
    {
        public const ServerPermission EveryoneDefault = ServerPermission.ViewChannels
            | ServerPermission.SendMessages
            | ServerPermission.Connect
            | ServerPermission.Speak;

        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IServerRoleRepository _roles;
        private readonly IServerMemberRoleRepository _memberRoles;
        private readonly IServerOverwriteRepository _overwrites;

        public ServerPermissionService(IServerRepository servers,
                                       IServerMemberRepository members,
                                       IServerRoleRepository roles,
                                       IServerMemberRoleRepository memberRoles,
                                       IServerOverwriteRepository overwrites)
        {
            _servers = servers;
            _members = members;
            _roles = roles;
            _memberRoles = memberRoles;
            _overwrites = overwrites;
        }

        public async Task<ServerPermission> GetAsync(string userId, int serverId)
        {
            var server = await _servers.GetByIdAsync(serverId);
            if (server is null)
            {
                return ServerPermission.None;
            }

            if (server.OwnerId == userId)
            {
                return ServerPermission.All;
            }

            if (await _members.GetAsync(serverId, userId) is null)
            {
                return ServerPermission.None;
            }

            var bits = (long)EveryoneDefault;
            var everyone = await _roles.GetEveryoneAsync(serverId);
            if (everyone is not null)
            {
                bits = everyone.Permissions;
            }

            foreach (var roleBits in await _memberRoles.GetPermissionBitsAsync(serverId, userId))
            {
                bits |= roleBits;
            }

            return (ServerPermission)bits & ServerPermission.All;
        }

        public async Task<ChannelPermissionContext> GetContextAsync(string userId, int serverId, List<ServerOverwrite>? overwrites = null)
        {
            var server = await _servers.GetByIdAsync(serverId);
            var everyone = await _roles.GetEveryoneAsync(serverId);
            return new ChannelPermissionContext
            {
                UserId = userId,
                IsOwner = server is not null && server.OwnerId == userId,
                Base = await GetAsync(userId, serverId),
                RoleIds = await _memberRoles.GetRoleIdsAsync(serverId, userId),
                EveryoneRoleId = everyone?.Id,
                Overwrites = overwrites ?? await _overwrites.GetForServerAsync(serverId)
            };
        }

        public async Task<ServerPermission> GetInChannelAsync(string userId, int serverId, ServerChannel channel)
        {
            var context = await GetContextAsync(userId, serverId);
            if (context.Base == ServerPermission.None && !context.IsOwner)
            {
                return ServerPermission.None;
            }

            return context.ForChannel(channel);
        }

        public async Task<bool> CanViewChannelAsync(string userId, int serverId, ServerChannel channel)
        {
            var perms = await GetInChannelAsync(userId, serverId, channel);
            return (perms & ServerPermission.ViewChannels) == ServerPermission.ViewChannels;
        }

        public async Task EnsureAsync(string userId, int serverId, ServerPermission needed)
        {
            var has = await GetAsync(userId, serverId);
            if ((has & needed) != needed)
            {
                throw new ForbiddenException("You don't have permission to do that.");
            }
        }

        public async Task EnsureEveryoneAsync(int serverId)
        {
            if (await _roles.GetEveryoneAsync(serverId) is not null)
            {
                return;
            }

            await _roles.AddAsync(new ServerRole
            {
                ServerId = serverId,
                Name = "@everyone",
                Color = "gray",
                Permissions = (long)EveryoneDefault,
                Position = 0,
                IsEveryone = true
            });
        }
    }
}
