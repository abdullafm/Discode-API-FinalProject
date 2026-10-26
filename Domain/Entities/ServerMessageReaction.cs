using Domain.Common;

namespace Domain.Entities
{
    public class ServerMessageReaction : BaseEntity
    {
        public int ServerMessageId { get; set; }
        public ServerMessage ServerMessage { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string Emoji { get; set; } = string.Empty;
    }
}
