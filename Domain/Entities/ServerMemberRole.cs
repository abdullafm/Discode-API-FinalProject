using Domain.Common;

namespace Domain.Entities
{
    public class ServerMemberRole : BaseEntity
    {
        public int ServerMemberId { get; set; }
        public ServerMember ServerMember { get; set; } = null!;
        public int RoleId { get; set; }
        public ServerRole Role { get; set; } = null!;
    }
}
