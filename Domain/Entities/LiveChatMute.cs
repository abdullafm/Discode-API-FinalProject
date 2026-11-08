using Domain.Common;

namespace Domain.Entities
{
    // A person who may only watch the streams of one host: no chat, no asking to join. It stays until it runs out
    // (ExpiresAt) or, when ExpiresAt is empty, until the host or a moderator takes it off the list.
    public class LiveChatMute : BaseEntity
    {
        public string HostId { get; set; } = string.Empty;
        public AppUser Host { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string MutedById { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }
}
