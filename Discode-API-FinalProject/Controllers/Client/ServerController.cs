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
