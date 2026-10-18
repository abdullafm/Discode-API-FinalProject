namespace Service.Helpers.DTOs.Live
{
    public class LiveJoinDto
    {
        public LiveStreamDto Stream { get; set; } = null!;
        public string HostConnectionId { get; set; } = string.Empty;
    }
}
