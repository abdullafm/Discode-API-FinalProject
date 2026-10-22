using Domain.Common;
using Domain.Enums;

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
        public DateTime? PinnedAt { get; set; }
        public int? ReplyToId { get; set; }
        public Message? ReplyTo { get; set; }
        public MessageForward? Forward { get; set; }
        public bool Undelivered { get; set; }
        public MessageKind Kind { get; set; }
        public int? CallSeconds { get; set; }
        public bool CallMissed { get; set; }
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
