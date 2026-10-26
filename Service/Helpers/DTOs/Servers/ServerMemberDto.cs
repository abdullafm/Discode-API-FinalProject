using Service.Helpers.DTOs.ServerRoles;

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
        public DateTime JoinedAt { get; set; }
        public DateTime AccountCreatedAt { get; set; }
        public string JoinMethod { get; set; } = string.Empty;
        public List<ServerRoleRefDto> Roles { get; set; } = new();
        public bool IsSelf { get; set; }
    }
}
