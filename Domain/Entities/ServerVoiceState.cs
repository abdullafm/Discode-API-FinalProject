using Domain.Common;

namespace Domain.Entities
{
    public class ServerVoiceState : BaseEntity
    {
        public int ServerId { get; set; }
        public int ChannelId { get; set; }
        public ServerChannel Channel { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsCameraOn { get; set; }
        public bool IsStreaming { get; set; }
        public DateTime SessionStartedAt { get; set; } = DateTime.UtcNow;
    }
}
