using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class ServerAuditLog : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string ActorId { get; set; } = string.Empty;
        public AppUser Actor { get; set; } = null!;
        public ServerAuditAction Action { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
