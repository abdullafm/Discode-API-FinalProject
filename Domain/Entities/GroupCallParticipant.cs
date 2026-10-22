using Domain.Common;

namespace Domain.Entities
{
    public class GroupCallParticipant : BaseEntity
    {
        public int GroupCallId { get; set; }
        public GroupCall GroupCall { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public bool Screen { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
    }
}
