namespace Service.Helpers.DTOs.Servers
{
    public class ServerInviteDto
    {
        public int ServerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int OnlineCount { get; set; }
        public bool IsMember { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
