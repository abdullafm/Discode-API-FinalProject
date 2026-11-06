using Domain.Common;

namespace Domain.Entities
{
    public class Call : BaseEntity
    {
        public string CallerId { get; set; } = string.Empty;
        public AppUser Caller { get; set; } = null!;
        public string CalleeId { get; set; } = string.Empty;
        public AppUser Callee { get; set; } = null!;
        public bool Video { get; set; }
        public bool Active { get; set; }
        public bool CallerIn { get; set; } = true;
        public bool CalleeIn { get; set; } = true;
        public bool CallerJoinable { get; set; }
        public bool CalleeJoinable { get; set; }
        public bool CallerScreen { get; set; }
        public bool CalleeScreen { get; set; }
        public int MessageId { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
