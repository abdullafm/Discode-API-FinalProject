namespace Service.Helpers.DTOs.ServerVoice
{
    public class UpdateServerVoiceDto
    {
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsCameraOn { get; set; }
        public bool IsStreaming { get; set; }
    }
}
