namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceModerationDto
    {
        public ServerVoiceChangeDto Change { get; set; } = new();
        public string TargetUserId { get; set; } = string.Empty;
        // muted, unmuted, deafened, undeafened or disconnected: what the target is told
        public string Kind { get; set; } = string.Empty;
    }
}
