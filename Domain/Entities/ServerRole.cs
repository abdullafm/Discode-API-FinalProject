using Domain.Common;

namespace Domain.Entities
{
    public class ServerRole : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "gray";
        public long Permissions { get; set; }
        public int Position { get; set; }
        public bool IsEveryone { get; set; }
        public bool DisplaySeparately { get; set; }
        public ICollection<ServerMemberRole> MemberRoles { get; set; } = new List<ServerMemberRole>();
    }
}
