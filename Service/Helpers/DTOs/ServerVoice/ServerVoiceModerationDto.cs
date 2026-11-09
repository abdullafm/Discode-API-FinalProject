namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceModerationDto
    {
        public ServerVoiceChangeDto Change { get; set; } = new();
        public string TargetUserId { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
    }
}
