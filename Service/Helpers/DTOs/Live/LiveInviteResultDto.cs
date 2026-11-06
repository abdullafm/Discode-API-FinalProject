namespace Service.Helpers.DTOs.Live
{
    public class LiveInviteResultDto
    {
        public int StreamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public List<string> UserNames { get; set; } = new();
    }
}
