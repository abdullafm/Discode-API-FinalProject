using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerController(IServerService serverService, IHubContext<PresenceHub> hubContext)
        {
            _serverService = serverService;
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
            return Ok(change.Server);
        }

        [HttpPut("{id:int}/banner")]
        public async Task<IActionResult> UpdateBanner(int id, [FromBody] UpdateServerBannerDto model)
        {
            var change = await _serverService.UpdateBannerAsync(UserId, id, model);
            await _hubContext.Clients.All.SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
            return Ok(change.Server);
        }

        [HttpPut("{id:int}/invite")]
        public async Task<IActionResult> UpdateInvite(int id, [FromBody] UpdateServerInviteDto model)
        {
            var change = await _serverService.UpdateInviteAsync(UserId, id, model);
            await _hubContext.Clients.All.SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
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

        private async Task PublishAsync(ServerChangeDto change)
        {
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            await _hubContext.Clients.Users(change.MemberIds).SendAsync("ServerUpdated", new { serverId = change.Server.Id, memberCount = change.Server.MemberCount });
        }
    }
}
