using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Presence;
using Service.Services.Interfaces;

namespace Service.Services
{
    // "X is typing...": works out who should see it. Nothing is stored.
    public class TypingService : ITypingService
    {
        private readonly IAccountRepository _accounts;
        private readonly IUserBlockRepository _blocks;
        private readonly IGroupRepository _groups;
        private readonly IServerChannelRepository _channels;
        private readonly IServerMemberRepository _members;
        private readonly IServerPermissionService _permissions;

        public TypingService(IAccountRepository accounts,
                             IUserBlockRepository blocks,
                             IGroupRepository groups,
                             IServerChannelRepository channels,
                             IServerMemberRepository members,
                             IServerPermissionService permissions)
        {
            _accounts = accounts;
            _blocks = blocks;
            _groups = groups;
            _channels = channels;
            _members = members;
            _permissions = permissions;
        }

        public async Task<TypingDto?> ResolveAsync(string userId, string kind, string key)
        {
            kind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            key = (key ?? string.Empty).Trim();
            if (key.Length == 0 || key.Length > 60)
            {
                return null;
            }

            var me = await _accounts.GetByIdAsync(userId);
            if (me is null || me.IsBanned || me.IsDeleted)
            {
                return null;
            }

            var result = new TypingDto
            {
                Kind = kind,
                UserName = me.UserName ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(me.DisplayName) ? me.UserName ?? string.Empty : me.DisplayName
            };

            switch (kind)
            {
                case "dm":
                    var other = await _accounts.GetByUserNameAsync(key);
                    if (other is null || other.Id == userId || other.IsSystem || await _blocks.IsBlockedEitherWayAsync(userId, other.Id))
                    {
                        return null;
                    }

                    // the other person knows this chat by the name of the one who writes
                    result.Key = result.UserName.ToLowerInvariant();
                    result.AudienceIds.Add(other.Id);
                    return result;

                case "group":
                    if (!int.TryParse(key, out var groupId))
                    {
                        return null;
                    }

                    var group = await _groups.GetWithMembersAsync(groupId);
                    if (group is null || group.Members.All(m => m.UserId != userId))
                    {
                        return null;
                    }

                    result.Key = groupId.ToString();
                    result.AudienceIds.AddRange(group.Members.Select(m => m.UserId).Where(id => id != userId));
                    return result;

                case "channel":
                    var parts = key.Split(':');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out var serverId) || !int.TryParse(parts[1], out var channelId))
                    {
                        return null;
                    }

                    var membership = await _members.GetAsync(serverId, userId);
                    var channel = await _channels.GetInServerAsync(serverId, channelId);
                    if (membership is null || channel is null || channel.Kind == ServerChannelKind.Voice)
                    {
                        return null;
                    }

                    // only somebody who can write in the channel is shown as typing
                    var perms = await _permissions.GetInChannelAsync(userId, serverId, channel);
                    var needed = channel.Kind == ServerChannelKind.Announcement || channel.IsReadOnly ? ServerPermission.ManageMessages : ServerPermission.SendMessages;
                    if ((perms & ServerPermission.ViewChannels) != ServerPermission.ViewChannels || (perms & needed) != needed)
                    {
                        return null;
                    }

                    result.Key = serverId + ":" + channelId;
                    result.AudienceIds.AddRange((await _permissions.GetChannelViewerIdsAsync(serverId, channel)).Where(id => id != userId));
                    return result;

                default:
                    return null;
            }
        }
    }
}
