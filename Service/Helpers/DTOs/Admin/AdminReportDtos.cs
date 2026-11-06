namespace Service.Helpers.DTOs.Admin
{
    public class AdminReportDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string Snapshot { get; set; } = string.Empty;
        public string ReporterUserName { get; set; } = string.Empty;
        public string ReporterDisplayName { get; set; } = string.Empty;
        public string ReporterAvatarUrl { get; set; } = string.Empty;
        public string? ReporterId { get; set; }
        public string? OwnerId { get; set; }
        public string OwnerUserName { get; set; } = string.Empty;
        public string OwnerDisplayName { get; set; } = string.Empty;
        public string OwnerAvatarUrl { get; set; } = string.Empty;
        public int? ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string ReviewedBy { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
    }

    public class AdminReportListDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int OpenCount { get; set; }
        public int ReviewedCount { get; set; }
        public int DismissedCount { get; set; }
        public List<AdminReportDto> Items { get; set; } = new();
    }

    public class SetReportStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
