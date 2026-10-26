using Domain.Common;

namespace Domain.Entities
{
    public class ServerChannelAccess : BaseEntity
    {
        public int ChannelId { get; set; }
        public ServerChannel Channel { get; set; } = null!;
        public int? RoleId { get; set; }
        public string? UserId { get; set; }
    }
}
