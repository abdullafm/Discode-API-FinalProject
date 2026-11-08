namespace Service.Helpers.DTOs.Live
{
    // One person on a host's chat-mute list
    public class LiveMuteDto
    {
        public int StreamId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        // empty = until the host or a moderator takes it off
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // What a moderation action did, so the hub can tell the people it touched
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

    // A viewer who was sent away from a stream (somebody blocked somebody)
    public class LiveEjectedDto
    {
        public int StreamId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public int ViewerCount { get; set; }
    }
}
