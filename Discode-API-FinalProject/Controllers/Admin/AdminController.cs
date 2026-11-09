using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Discode_API_FinalProject.Hubs.PresenceHub> _hubContext;
        private readonly Discode_API_FinalProject.Hubs.DiscoverBroadcaster _discover;
        private readonly Repository.Notifications.IAdminChangeNotifier _changes;

        public AdminController(IAdminService adminService,
                               Microsoft.AspNetCore.SignalR.IHubContext<Discode_API_FinalProject.Hubs.PresenceHub> hubContext,
                               Discode_API_FinalProject.Hubs.DiscoverBroadcaster discover,
                               Repository.Notifications.IAdminChangeNotifier changes)
        {
            _adminService = adminService;
            _hubContext = hubContext;
            _discover = discover;
            _changes = changes;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet("activity")]
        public async Task<IActionResult> Activity([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter, [FromServices] IUserModerationService moderation)
        {
            return Ok(await moderation.GetActivityAsync(filter));
        }

        [HttpGet("servers/{id:int}")]
        public async Task<IActionResult> GetServer(int id)
        {
            return Ok(await _adminService.GetServerAsync(id));
        }

        [HttpGet("servers/{id:int}/members")]
        public async Task<IActionResult> GetServerMembers(int id, [FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.GetServerMembersAsync(id, filter));
        }

        [HttpGet("servers/{id:int}/members/search")]
        public async Task<IActionResult> SearchServerMembers(int id, [FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.SearchServerMembersAsync(id, filter));
        }

        [HttpPost("servers/{id:int}/warn")]
        public async Task<IActionResult> WarnServer(int id, [FromBody] Service.Helpers.DTOs.Admin.BanServerDto model, [FromServices] Discode_API_FinalProject.Hubs.SystemMessenger messenger)
        {
            var (ownerId, serverName) = await _adminService.WarnServerAsync(UserId, id, model);
            await messenger.SendAsync(ownerId, $"Your server \"{serverName}\" received a warning from the Discode team.\n\nReason: {model.Reason.Trim()}\n\nPlease review the server and follow the community rules.");
            await _changes.NotifyAsync(new[] { "servers", "activity" }, new[] { id });
            return Ok();
        }

        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost("servers/{id:int}/ban")]
        public async Task<IActionResult> BanServer(int id, [FromBody] Service.Helpers.DTOs.Admin.BanServerDto model, [FromServices] IBoostService boosts)
        {
            var boosters = await boosts.GetBoosterIdsAsync(id);
            var memberIds = await _adminService.BanServerAsync(UserId, id, model);
            await Discode_API_FinalProject.Helpers.NitroNotifier.TellAsync(_hubContext, boosts, new Service.Helpers.DTOs.Nitro.NitroChangeDto { UserIds = boosters.ToList() });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerRemoved", new { serverId = id, reason = "banned" });
            await _discover.PublishServerAsync(id);
            await _changes.NotifyAsync(new[] { "servers" }, new[] { id });
            return Ok();
        }

        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost("servers/{id:int}/unban")]
        public async Task<IActionResult> UnbanServer(int id)
        {
            var memberIds = await _adminService.UnbanServerAsync(UserId, id);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServersChanged");
            await _discover.PublishServerAsync(id);
            await _changes.NotifyAsync(new[] { "servers" }, new[] { id });
            return Ok();
        }

        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpDelete("servers/{id:int}")]
        public async Task<IActionResult> DeleteServer(int id, [FromServices] IBoostService boosts)
        {
            var boosters = await boosts.GetBoosterIdsAsync(id);
            var memberIds = await _adminService.DeleteServerAsync(UserId, id);
            await Discode_API_FinalProject.Helpers.NitroNotifier.TellAsync(_hubContext, boosts, new Service.Helpers.DTOs.Nitro.NitroChangeDto { UserIds = boosters.ToList() });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerRemoved", new { serverId = id, reason = "deleted" });
            await _discover.PublishRemovedAsync(id);
            return Ok();
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.GetUsersAsync(filter));
        }

        [HttpGet("servers")]
        public async Task<IActionResult> GetAllServers([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.GetAllServersAsync(filter));
        }

        [HttpGet("servers/search")]
        public async Task<IActionResult> SearchServers([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.SearchServersAsync(filter));
        }

        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            return Ok(await _adminService.GetOverviewAsync());
        }
    }
}
