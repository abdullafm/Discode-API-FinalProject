using Domain.Common;

namespace Domain.Entities
{
    public class ServerTemplate : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public string Snapshot { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }
}
