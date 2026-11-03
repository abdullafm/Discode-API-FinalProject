namespace Service.Helpers.DTOs.ServerVoice
{
    public class ServerVoiceDto
    {
        public List<ServerVoiceChannelDto> Channels { get; set; } = new();
        public ServerVoiceCurrentDto? Current { get; set; }
        // the viewer may mute, deafen and disconnect other people here (owner, or the Mute Members permission)
        public bool CanModerate { get; set; }
    }
}
