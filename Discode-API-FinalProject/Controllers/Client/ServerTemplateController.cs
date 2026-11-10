using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Discode_API_FinalProject.Hubs;
using Service.Helpers.DTOs.ServerTemplates;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Controllers.Client
{
    [ApiController]
    [Authorize]
    public class ServerTemplateController : ControllerBase
    {
        private readonly IServerTemplateService _templates;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerTemplateController(IServerTemplateService templates, IHubContext<PresenceHub> hubContext)
        {
            _templates = templates;
            _hubContext = hubContext;
        }

        private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("api/server/{serverId:int}/template")]
        public async Task<IActionResult> Mine(int serverId)
        {
            var template = await _templates.GetAsync(UserId, serverId);
            return template is null ? NoContent() : Ok(template);
        }

        [HttpPost("api/server/{serverId:int}/template")]
        public async Task<IActionResult> Save(int serverId, [FromBody] SaveServerTemplateDto model)
        {
            var template = await _templates.SaveAsync(UserId, serverId, model);
            await _hubContext.Clients.All.SendAsync("ServerAuditChanged", new { serverId });
            return Ok(template);
        }

        [HttpPost("api/server/{serverId:int}/template/sync")]
        public async Task<IActionResult> Sync(int serverId)
        {
            return Ok(await _templates.SyncAsync(UserId, serverId));
        }

        [HttpDelete("api/server/{serverId:int}/template")]
        public async Task<IActionResult> Remove(int serverId)
        {
            await _templates.DeleteAsync(UserId, serverId);
            return NoContent();
        }

        [HttpGet("api/template/{code}")]
        public async Task<IActionResult> Info(string code)
        {
            return Ok(await _templates.GetInfoAsync(code));
        }

        [HttpPost("api/template/{code}/use")]
        public async Task<IActionResult> Use(string code, [FromBody] UseServerTemplateDto model)
        {
            var change = await _templates.UseAsync(UserId, code, model);
            await _hubContext.Clients.User(UserId).SendAsync("ServersChanged");
            return Ok(change.Server);
        }
    }
}
