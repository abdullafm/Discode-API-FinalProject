using Domain.Common;

namespace Domain.Entities
{
    // A boost that was taken off a server: its place is not free again until AvailableAt
    public class BoostCooldown : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTime AvailableAt { get; set; }
    }
}
