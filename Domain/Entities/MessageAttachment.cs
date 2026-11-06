using Domain.Common;

namespace Domain.Entities
{
    public class MessageAttachment : BaseEntity
    {
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
