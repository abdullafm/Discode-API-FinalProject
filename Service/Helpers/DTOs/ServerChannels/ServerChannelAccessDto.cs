using Service.Helpers.DTOs.ServerRoles;

namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerChannelAccessDto
    {
        public List<ServerRoleRefDto> Roles { get; set; } = new();
        public List<ServerChannelAccessMemberDto> Members { get; set; } = new();
    }
}
