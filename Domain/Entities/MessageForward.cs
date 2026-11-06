using Domain.Common;

namespace Domain.Entities
{
    public class MessageForward : BaseEntity
    {
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;
        public string OriginalSenderId { get; set; } = string.Empty;
        public AppUser OriginalSender { get; set; } = null!;
    }
}
