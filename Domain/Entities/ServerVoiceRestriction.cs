using Domain.Common;

namespace Domain.Entities
{
    public class ServerVoiceRestriction : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public bool Muted { get; set; }
        public bool Deafened { get; set; }
        public string ByUserId { get; set; } = string.Empty;
    }
}
