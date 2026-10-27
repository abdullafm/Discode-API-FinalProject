using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.ServerAudit;
using Service.Helpers.DTOs.ServerBans;
using Service.Helpers.DTOs.Servers;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ServerController : ControllerBase
    {
        private readonly IServerService _serverService;
        private readonly IServerAuditService _auditService;
        private readonly IServerBanService _banService;
        private readonly IServerTimeoutService _timeoutService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerController(IServerService serverService, IServerAuditService auditService, IServerBanService banService, IServerTimeoutService timeoutService, IHubContext<PresenceHub> hubContext)
        {
            _serverService = serverService;
            _auditService = auditService;
            _banService = banService;
            _timeoutService = timeoutService;
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
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _serverService.GetMineAsync(UserId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await _serverService.GetAsync(UserId, id));
        }

        [HttpGet("invite/{code}")]
        public async Task<IActionResult> GetInvite(string code)
        {
            return Ok(await _serverService.GetInviteAsync(UserId, code));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServerDto model)
        {
            var change = await _serverService.CreateAsync(UserId, model);
            await PublishAsync(change);
            return Ok(change.Server);
        }

        [HttpPut("{id:int}/preferences")]
        public async Task<IActionResult> UpdatePreferences(int id, [FromBody] UpdateServerPreferencesDto model)
        {
            var server = await _serverService.UpdatePreferencesAsync(UserId, id, model);
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            return Ok(server);
        }

        [HttpGet("{id:int}/audit")]
        public async Task<IActionResult> GetAudit(int id, [FromQuery] ServerAuditFilterDto filter)
        {
            return Ok(await _auditService.GetAsync(UserId, id, filter));
        }

        [HttpGet("{id:int}/bans")]
        public async Task<IActionResult> GetBans(int id, [FromQuery] ServerBanFilterDto filter)
        {
            return Ok(await _banService.GetBansAsync(UserId, id, filter));
        }

        [HttpPost("{id:int}/bans")]
        public async Task<IActionResult> Ban(int id, [FromBody] BanServerMemberDto model)
        {
            var ban = await _banService.BanAsync(UserId, id, model);
            await RemovedAsync(id, ban.UserId, "banned");
            return Ok(ban);
        }

        [HttpDelete("{id:int}/bans/{userId}")]
        public async Task<IActionResult> Unban(int id, string userId)
        {
            await _banService.UnbanAsync(UserId, id, userId);
            await NotifyBansAsync(id);
            return Ok();
        }

        [HttpPut("{id:int}/members/{userId}/timeout")]
        public async Task<IActionResult> Timeout(int id, string userId, [FromBody] TimeoutServerMemberDto model)
        {
            await _timeoutService.SetAsync(UserId, id, userId, model);
            var memberIds = await _timeoutService.GetMemberIdsAsync(id);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerRolesChanged", new { serverId = id });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerAuditChanged", new { serverId = id });
            return Ok();
        }

        [HttpDelete("{id:int}/members/{userId}")]
        public async Task<IActionResult> Kick(int id, string userId)
        {
            await _banService.KickAsync(UserId, id, userId);
            await RemovedAsync(id, userId, "kicked");
            return Ok();
        }

        [HttpPost("{id:int}/delete")]
        public async Task<IActionResult> Delete(int id, [FromBody] DeleteServerDto model)
        {
            var memberIds = await _serverService.DeleteAsync(UserId, id, model);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerRemoved", new { serverId = id, reason = "deleted" });
            return Ok();
        }

        [HttpGet("{id:int}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            return Ok(await _serverService.GetMembersAsync(UserId, id));
        }

        [HttpPut("{id:int}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateServerProfileDto model)
        {
            var change = await _serverService.UpdateProfileAsync(UserId, id, model);
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServersChanged");
            await _hubContext.Clients.All.SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            await _hubContext.Clients.All.SendAsync("ServerAuditChanged", new { serverId = change.Server.Id });
            return Ok(change.Server);
        }

        [HttpPut("{id:int}/banner")]
        public async Task<IActionResult> UpdateBanner(int id, [FromBody] UpdateServerBannerDto model)
        {
            var change = await _serverService.UpdateBannerAsync(UserId, id, model);
            await _hubContext.Clients.All.SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            await _hubContext.Clients.All.SendAsync("ServerAuditChanged", new { serverId = change.Server.Id });
            return Ok(change.Server);
        }

        [HttpPut("{id:int}/invite")]
        public async Task<IActionResult> UpdateInvite(int id, [FromBody] UpdateServerInviteDto model)
        {
            var change = await _serverService.UpdateInviteAsync(UserId, id, model);
            await _hubContext.Clients.All.SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            await _hubContext.Clients.All.SendAsync("ServerAuditChanged", new { serverId = change.Server.Id });
            return Ok(change.Server);
        }

        [HttpPut("order")]
        public async Task<IActionResult> Reorder([FromBody] ReorderServersDto model)
        {
            await _serverService.ReorderAsync(UserId, model);
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            return Ok();
        }

        [HttpPost("{id:int}/leave")]
        public async Task<IActionResult> Leave(int id)
        {
            var change = await _serverService.LeaveAsync(UserId, id);
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServerAuditChanged", new { serverId = change.Server.Id });
            return Ok();
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] JoinServerDto model)
        {
            var change = await _serverService.JoinAsync(UserId, model);
            if (change.Joined)
            {
                await PublishAsync(change);
            }

            return Ok(change.Server);
        }

        private async Task RemovedAsync(int serverId, string targetUserId, string reason)
        {
            var memberIds = await _banService.GetMemberIdsAsync(serverId);
            await _hubContext.Clients.User(targetUserId).SendAsync("ServersChanged");
            await _hubContext.Clients.User(targetUserId).SendAsync("ServerRemoved", new { serverId, reason });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerUpdated", new { serverId, memberCount = memberIds.Count });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerAuditChanged", new { serverId });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerBansChanged", new { serverId });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerVoiceChanged", new { serverId });
        }

        private async Task NotifyBansAsync(int serverId)
        {
            var memberIds = await _banService.GetMemberIdsAsync(serverId);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerAuditChanged", new { serverId });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerBansChanged", new { serverId });
        }

        private async Task PublishAsync(ServerChangeDto change)
        {
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServerAuditChanged", new { serverId = change.Server.Id });
        }
    }
}
