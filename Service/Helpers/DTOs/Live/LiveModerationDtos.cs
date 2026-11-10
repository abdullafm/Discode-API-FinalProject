namespace Service.Helpers.DTOs.Live
{
    public class LiveMuteDto
    {
        public int StreamId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LiveModerationResultDto
    {
        public int StreamId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsModerator { get; set; }
        public bool ModeratorRemoved { get; set; }
        public bool Muted { get; set; }
        public DateTime? MutedUntil { get; set; }
        public List<string> TargetConnectionIds { get; set; } = new();
        public string? EndedGuestConnectionId { get; set; }
    }

    public class LiveEjectedDto
    {
        public int StreamId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public int ViewerCount { get; set; }
    }
}
