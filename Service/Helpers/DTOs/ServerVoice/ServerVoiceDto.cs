namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceDto
    {
        public List<ServerVoiceChannelDto> Channels { get; set; } = new();
        public ServerVoiceCurrentDto? Current { get; set; }
    }
}
