namespace Service.Helpers.DTOs.Servers
{
    public class ServerMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public bool IsOwner { get; set; }
        public bool IsSelf { get; set; }
    }
}
