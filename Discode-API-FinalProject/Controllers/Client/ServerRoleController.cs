using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.ServerRoles;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/server/{serverId:int}/roles")]
    [ApiController]
    [Authorize]
    public class ServerRoleController : ControllerBase
    {
        private readonly IServerRoleService _roleService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerRoleController(IServerRoleService roleService, IHubContext<PresenceHub> hubContext)
        {
            _roleService = roleService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int serverId)
        {
            return Ok(await _roleService.GetRolesAsync(UserId, serverId));
        }

        [HttpPost]
        public async Task<IActionResult> Create(int serverId, [FromBody] CreateServerRoleDto model)
        {
            var role = await _roleService.CreateAsync(UserId, serverId, model);
            await NotifyAsync(serverId);
            return Ok(role);
        }

        [HttpPut("{roleId:int}")]
        public async Task<IActionResult> Update(int serverId, int roleId, [FromBody] UpdateServerRoleDto model)
        {
            var role = await _roleService.UpdateAsync(UserId, serverId, roleId, model);
            await NotifyAsync(serverId);
            return Ok(role);
        }

        [HttpDelete("{roleId:int}")]
        public async Task<IActionResult> Delete(int serverId, int roleId)
        {
            await _roleService.DeleteAsync(UserId, serverId, roleId);
            await NotifyAsync(serverId);
            return Ok();
        }

        [HttpGet("{roleId:int}/members")]
        public async Task<IActionResult> GetMembers(int serverId, int roleId)
        {
            return Ok(await _roleService.GetRoleMembersAsync(UserId, serverId, roleId));
        }

        [HttpPut("{roleId:int}/members/{userId}")]
        public async Task<IActionResult> Assign(int serverId, int roleId, string userId)
        {
            await _roleService.AssignAsync(UserId, serverId, roleId, userId);
            await NotifyAsync(serverId);
            return Ok();
        }

        [HttpDelete("{roleId:int}/members/{userId}")]
        public async Task<IActionResult> Remove(int serverId, int roleId, string userId)
        {
            await _roleService.RemoveAsync(UserId, serverId, roleId, userId);
            await NotifyAsync(serverId);
            return Ok();
        }

        private async Task NotifyAsync(int serverId)
        {
            var memberIds = await _roleService.GetMemberIdsAsync(serverId);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerRolesChanged", new { serverId });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerAuditChanged", new { serverId });
        }
    }
}
