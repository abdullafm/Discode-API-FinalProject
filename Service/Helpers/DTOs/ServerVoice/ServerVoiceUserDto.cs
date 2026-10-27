namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
    }
}
