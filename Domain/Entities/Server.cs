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
        // the short link people can type instead of the invite code: a perk of a server with 14 boosts
        public string? CustomLink { get; set; }
        public string BannerColor { get; set; } = "ink";
        public bool IsPrivate { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? BannedAt { get; set; }
        public string BanReason { get; set; } = string.Empty;
        public string? BannedById { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public AppUser Owner { get; set; } = null!;
        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<ServerRole> Roles { get; set; } = new List<ServerRole>();
    }
}
