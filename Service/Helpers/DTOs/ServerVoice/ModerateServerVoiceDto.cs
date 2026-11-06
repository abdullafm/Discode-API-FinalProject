namespace Service.Helpers.DTOs.ServerVoice
{
    // null = leave that part as it is
    public class ModerateServerVoiceDto
    {
        public bool? ServerMuted { get; set; }
        public bool? ServerDeafened { get; set; }
    }
}
