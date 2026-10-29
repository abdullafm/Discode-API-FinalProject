using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Report : BaseEntity
    {
        public string ReporterId { get; set; } = string.Empty;
        public AppUser Reporter { get; set; } = null!;
        public ReportTargetType TargetType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string? TargetOwnerId { get; set; }
        public ReportReason Reason { get; set; }
        public string Details { get; set; } = string.Empty;
        public string ContentSnapshot { get; set; } = string.Empty;
        public int? ContextServerId { get; set; }
        public int? ContextGroupId { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Open;
    }
}
