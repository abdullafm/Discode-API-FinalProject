using Domain.Entities;
using Domain.Enums;

namespace Service.Helpers
{
    public class ChannelPermissionContext
    {
        public const ServerPermission ChannelBits = ServerPermission.ViewChannels
            | ServerPermission.SendMessages
            | ServerPermission.ManageMessages
            | ServerPermission.MentionEveryone
            | ServerPermission.Connect
            | ServerPermission.Speak;

        public string UserId { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public ServerPermission Base { get; set; }
        public HashSet<int> RoleIds { get; set; } = new();
        public int? EveryoneRoleId { get; set; }
        public List<ServerOverwrite> Overwrites { get; set; } = new();

        public ServerPermission ForChannel(ServerChannel channel)
        {
            return Apply(EffectiveFor(channel));
        }

        public ServerPermission ForCategory(int categoryId)
        {
            return Apply(Overwrites.Where(o => o.CategoryId == categoryId));
        }

        public IEnumerable<ServerOverwrite> EffectiveFor(ServerChannel channel)
        {
            if (channel.SyncWithCategory && channel.CategoryId is not null)
            {
                return Overwrites.Where(o => o.CategoryId == channel.CategoryId);
            }

            return Overwrites.Where(o => o.ChannelId == channel.Id);
        }

        private ServerPermission Apply(IEnumerable<ServerOverwrite> set)
        {
            if (IsOwner)
            {
                return ServerPermission.All;
            }

            var items = set.ToList();
            var bits = (long)Base;
            var mask = (long)ChannelBits;

            var everyone = items.FirstOrDefault(o => o.RoleId is not null && o.RoleId == EveryoneRoleId);
            if (everyone is not null)
            {
                bits = (bits & ~(everyone.Deny & mask)) | (everyone.Allow & mask);
            }

            long roleAllow = 0;
            long roleDeny = 0;
            foreach (var o in items.Where(o => o.RoleId is not null && o.RoleId != EveryoneRoleId && RoleIds.Contains(o.RoleId.Value)))
            {
                roleAllow |= o.Allow;
                roleDeny |= o.Deny;
            }

            bits = (bits & ~(roleDeny & mask)) | (roleAllow & mask);

            var mine = items.FirstOrDefault(o => o.UserId is not null && o.UserId == UserId);
            if (mine is not null)
            {
                bits = (bits & ~(mine.Deny & mask)) | (mine.Allow & mask);
            }

            if ((Base & ServerPermission.ManageChannels) == ServerPermission.ManageChannels)
            {
                bits |= (long)ServerPermission.ViewChannels;
            }

            return (ServerPermission)bits & ServerPermission.All;
        }
    }
}
