using Domain.Common;

namespace Domain.Entities
{
    public class GroupMember : BaseEntity
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public DateTime? LastReadAt { get; set; }
    }
}
