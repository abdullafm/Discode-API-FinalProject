using Domain.Common;

namespace Domain.Entities
{
    // Somebody the host trusts to look after the chat of every stream the host opens
    public class LiveModerator : BaseEntity
    {
        public string HostId { get; set; } = string.Empty;
        public AppUser Host { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
