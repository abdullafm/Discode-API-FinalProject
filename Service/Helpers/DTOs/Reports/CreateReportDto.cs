namespace Service.Helpers.DTOs.Reports
{
    public class CreateReportDto
    {
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
