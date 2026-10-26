namespace Service.Helpers.DTOs.ServerAudit
{
    public class ServerAuditFilterDto
    {
        public string? ActorId { get; set; }
        public string? Action { get; set; }
        public int? BeforeId { get; set; }
        public int Take { get; set; } = 50;
    }
}
