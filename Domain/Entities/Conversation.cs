using Domain.Common;

namespace Domain.Entities
{
    public class Conversation : BaseEntity
    {
        public string UserAId { get; set; } = string.Empty;
        public AppUser UserA { get; set; } = null!;
        public string UserBId { get; set; } = string.Empty;
        public AppUser UserB { get; set; } = null!;
        public string LastMessageText { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public string LastSenderId { get; set; } = string.Empty;
        public int UserAUnread { get; set; }
        public int UserBUnread { get; set; }
        public bool UserAHidden { get; set; }
        public bool UserBHidden { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
