using Domain.Common;

namespace Domain.Entities
{
    public class Poll : BaseEntity
    {
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;
        public string Question { get; set; } = string.Empty;
        public bool AllowMultiple { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
