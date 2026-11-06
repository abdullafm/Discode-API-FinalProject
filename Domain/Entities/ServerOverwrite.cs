using Domain.Common;

namespace Domain.Entities
{
    public class ServerOverwrite : BaseEntity
    {
        public int ServerId { get; set; }
        public int? ChannelId { get; set; }
        public ServerChannel? Channel { get; set; }
        public int? CategoryId { get; set; }
        public int? RoleId { get; set; }
        public string? UserId { get; set; }
        public long Allow { get; set; }
        public long Deny { get; set; }
    }
}
