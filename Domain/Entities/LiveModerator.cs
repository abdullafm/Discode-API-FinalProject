using Domain.Common;

namespace Domain.Entities
{
    public class LiveModerator : BaseEntity
    {
        public string HostId { get; set; } = string.Empty;
        public AppUser Host { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
