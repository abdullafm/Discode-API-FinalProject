using Domain.Common;

namespace Domain.Entities
{
    public class ServerMessageAttachment : BaseEntity
    {
        public int ServerMessageId { get; set; }
        public ServerMessage ServerMessage { get; set; } = null!;
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
