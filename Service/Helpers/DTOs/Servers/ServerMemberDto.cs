using Service.Helpers.DTOs.ServerRoles;

namespace Service.Helpers.DTOs.Servers
{
    public class ServerMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CustomStatus { get; set; } = string.Empty;
        // the About text of the profile card; empty for a private profile unless it is mine
        public string Bio { get; set; } = string.Empty;
        public string Status { get; set; } = "offline";
        public bool IsOwner { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime AccountCreatedAt { get; set; }
        public string JoinMethod { get; set; } = string.Empty;
        public List<ServerRoleRefDto> Roles { get; set; } = new();
        public bool IsSelf { get; set; }
        public bool CanModerate { get; set; }
        public DateTime? TimeoutUntil { get; set; }
    }
}
