using Domain.Common;

namespace Domain.Entities
{
    public class Server : BaseEntity
    {
        public string PublicId { get; set; } = Domain.Common.PublicIdGenerator.New();
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string BannerColor { get; set; } = "ink";
        public bool IsPrivate { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public AppUser Owner { get; set; } = null!;
        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<ServerRole> Roles { get; set; } = new List<ServerRole>();
    }
}
