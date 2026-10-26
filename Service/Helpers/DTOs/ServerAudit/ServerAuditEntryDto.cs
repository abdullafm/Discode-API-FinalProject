namespace Service.Helpers.DTOs.ServerAudit
{
    public class ServerAuditEntryDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public string ActorUserName { get; set; } = string.Empty;
        public string ActorDisplayName { get; set; } = string.Empty;
        public string ActorAvatarUrl { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
