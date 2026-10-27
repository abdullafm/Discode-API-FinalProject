using Service.Helpers.DTOs.ServerRoles;
using Service.Helpers.DTOs.Servers;

namespace Service.Services.Interfaces
{
    public interface IServerRoleService
    {
        Task<IEnumerable<ServerRoleDto>> GetRolesAsync(string userId, int serverId);
        Task<ServerRoleDto> CreateAsync(string userId, int serverId, CreateServerRoleDto model);
        Task<ServerRoleDto> UpdateAsync(string userId, int serverId, int roleId, UpdateServerRoleDto model);
        Task DeleteAsync(string userId, int serverId, int roleId);
        Task<IEnumerable<ServerMemberDto>> GetRoleMembersAsync(string userId, int serverId, int roleId);
        Task AssignAsync(string userId, int serverId, int roleId, string targetUserId);
        Task RemoveAsync(string userId, int serverId, int roleId, string targetUserId);
        Task ReorderAsync(string userId, int serverId, ReorderServerRolesDto model);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
