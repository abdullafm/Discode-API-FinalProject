using Domain.Common;

namespace Domain.Entities
{
    public class ServerBan : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string BannedById { get; set; } = string.Empty;
        public AppUser BannedBy { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;
    }
}
