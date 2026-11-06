using Domain.Common;

namespace Domain.Entities
{
    public class GroupMessageAttachment : BaseEntity
    {
        public int GroupMessageId { get; set; }
        public GroupMessage GroupMessage { get; set; } = null!;
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
