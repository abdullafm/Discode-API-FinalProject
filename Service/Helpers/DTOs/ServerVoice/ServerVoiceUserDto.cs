namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsCameraOn { get; set; }
        public bool IsStreaming { get; set; }
        public bool IsSelf { get; set; }
    }
}
