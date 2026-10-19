using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string RecipientId { get; set; } = string.Empty;
        public AppUser Recipient { get; set; } = null!;
        public string ActorId { get; set; } = string.Empty;
        public AppUser Actor { get; set; } = null!;
        public NotificationType Type { get; set; }
        public int? PostId { get; set; }
        public Post? Post { get; set; }
        public int? CommentId { get; set; }
        public int? StoryId { get; set; }
        public Story? Story { get; set; }
        public string? Snippet { get; set; }
        public bool IsRead { get; set; }
    }
}
