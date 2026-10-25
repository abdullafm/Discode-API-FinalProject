using Domain.Common;

namespace Domain.Entities
{
    public class ServerMember : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
