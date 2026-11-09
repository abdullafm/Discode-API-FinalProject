namespace Service.Helpers.DTOs.Live
{
    public class LiveViewerDto
    {
        public int StreamId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsModerator { get; set; }
        public bool IsMuted { get; set; }
        public DateTime? MutedUntil { get; set; }
    }
}
