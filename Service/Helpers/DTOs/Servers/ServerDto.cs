namespace Service.Helpers.DTOs.Servers
{
    public class ServerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BannerColor { get; set; } = "ink";
        public bool IsPrivate { get; set; }
        public int OnlineCount { get; set; }
        public string OwnerUserName { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Position { get; set; }
        public bool Muted { get; set; }
        public string NotificationLevel { get; set; } = "mentions";
        public bool HideMutedChannels { get; set; }
        public bool ShowAllChannels { get; set; } = true;
        public List<ServerMemberDto> Members { get; set; } = new();
    }
}
