namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceCurrentDto
    {
        public int ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public int ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
    }
}
