using Domain.Common;

namespace Domain.Entities
{
    public class GroupMessage : BaseEntity
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }
}
