using Domain.Common;

namespace Domain.Entities
{
    public class Server : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public AppUser Owner { get; set; } = null!;
        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
    }
}
