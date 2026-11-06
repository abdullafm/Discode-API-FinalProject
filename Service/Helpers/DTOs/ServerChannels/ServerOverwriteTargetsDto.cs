using Service.Helpers.DTOs.ServerRoles;

namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerOverwriteTargetsDto
    {
        public List<ServerRoleRefDto> Roles { get; set; } = new();
        public List<ServerOverwriteMemberDto> Members { get; set; } = new();
    }
}
