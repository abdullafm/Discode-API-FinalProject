using Domain.Common;

namespace Domain.Entities
{
    public class MessageReaction : BaseEntity
    {
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string Emoji { get; set; } = string.Empty;
    }
}
