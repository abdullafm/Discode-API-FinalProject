using Domain.Common;

namespace Domain.Entities
{
    // Where a person stopped reading a chat: what came after is "new" the next time they open it
    public class ChatReadMarker : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        // "dm", "group" or "channel"
        public string Kind { get; set; } = string.Empty;
        // dm: the user name of the other person (lower case), group: the group id, channel: "serverId:channelId"
        public string Key { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
