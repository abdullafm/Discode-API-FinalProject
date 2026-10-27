namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceChannelDto
    {
        public int ChannelId { get; set; }
        public DateTime StartedAt { get; set; }
        public List<ServerVoiceUserDto> Users { get; set; } = new();
    }
}
