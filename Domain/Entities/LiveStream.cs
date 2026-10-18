using Domain.Common;

namespace Domain.Entities
{
    public class LiveStream : BaseEntity
    {
        public string HostId { get; set; } = string.Empty;
        public AppUser Host { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? EndedAt { get; set; }
    }
}
