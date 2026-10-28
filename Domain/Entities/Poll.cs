using Domain.Common;

namespace Domain.Entities
{
    public class Poll : BaseEntity
    {
        public int? MessageId { get; set; }
        public Message? Message { get; set; }
        public int? GroupMessageId { get; set; }
        public GroupMessage? GroupMessage { get; set; }
        public int? ServerMessageId { get; set; }
        public ServerMessage? ServerMessage { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool AllowMultiple { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
