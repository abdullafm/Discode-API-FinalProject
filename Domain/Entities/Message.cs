using Domain.Common;

namespace Domain.Entities
{
    public class Message : BaseEntity
    {
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public DateTime? EditedAt { get; set; }
<<<<<<< Updated upstream
=======
        public DateTime? PinnedAt { get; set; }
        public int? ReplyToId { get; set; }
        public Message? ReplyTo { get; set; }
        public MessageForward? Forward { get; set; }
>>>>>>> Stashed changes
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}
