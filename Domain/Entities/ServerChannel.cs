using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class ServerChannel : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public int? CategoryId { get; set; }
        public ServerCategory? Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public ServerChannelKind Kind { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsReadOnly { get; set; }
        public bool SyncWithCategory { get; set; } = true;
        public int Position { get; set; }
        public int UserLimit { get; set; }
    }
}
