using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class ServerMember : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public int Position { get; set; }
        public DateTime? MutedUntil { get; set; }
        public bool MutedIndefinitely { get; set; }
        public ServerNotificationLevel NotificationLevel { get; set; } = ServerNotificationLevel.Mentions;
        public bool HideMutedChannels { get; set; }
        public bool ShowAllChannels { get; set; } = true;
    }
}
