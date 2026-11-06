using Domain.Common;

namespace Domain.Entities
{
    public class ServerCategory : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public int Position { get; set; }
        public ICollection<ServerChannel> Channels { get; set; } = new List<ServerChannel>();
    }
}
