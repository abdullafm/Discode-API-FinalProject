using Domain.Common;

namespace Domain.Entities
{
    // One boost a Nitro member put on a server
    public class ServerBoost : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
