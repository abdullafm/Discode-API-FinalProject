using Domain.Common;

namespace Domain.Entities
{
    public class UserWarning : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string IssuedById { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime? SeenAt { get; set; }
    }
}
