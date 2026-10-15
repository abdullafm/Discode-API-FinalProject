using Domain.Common;

namespace Domain.Entities
{
    public class UserBlock : BaseEntity
    {
        public string BlockerId { get; set; } = string.Empty;
        public AppUser Blocker { get; set; } = null!;
        public string BlockedId { get; set; } = string.Empty;
        public AppUser Blocked { get; set; } = null!;
    }
}
