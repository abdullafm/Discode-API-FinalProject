namespace Service.Helpers.DTOs.Presence
{
    public class PresenceChangeDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public string PreferredStatus { get; set; } = "online";
        public bool ShouldBroadcast { get; set; }
        public IEnumerable<string> FriendIds { get; set; } = Array.Empty<string>();
    }
}
