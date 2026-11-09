using Domain.Common;

namespace Domain.Entities
{
    public class BoostCooldown : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTime AvailableAt { get; set; }
    }
}
