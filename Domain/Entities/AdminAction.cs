using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class AdminAction : BaseEntity
    {
        public string AdminId { get; set; } = string.Empty;
        public AppUser Admin { get; set; } = null!;
        public AdminTargetType TargetType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public AdminActionKind Kind { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
