using Domain.Common;

namespace Domain.Entities
{
    public class ChatReadMarker : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string Kind { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
