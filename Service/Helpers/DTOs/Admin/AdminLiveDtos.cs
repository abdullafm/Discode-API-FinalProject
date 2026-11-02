namespace Service.Helpers.DTOs.Admin
{
    public class AdminLiveStreamDto
    {
        public int Id { get; set; }
        public string HostId { get; set; } = string.Empty;
        public string HostUserName { get; set; } = string.Empty;
        public string HostDisplayName { get; set; } = string.Empty;
        public string HostAvatarUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int ViewerCount { get; set; }
    }

    public class AdminLiveListDto
    {
        public List<AdminLiveStreamDto> Items { get; set; } = new();
    }

    public class LiveReasonDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class LiveBanDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Minutes { get; set; }
    }
}
