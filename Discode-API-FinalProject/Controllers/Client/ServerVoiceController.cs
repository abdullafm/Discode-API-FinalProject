using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.ServerVoice;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ServerVoiceController : ControllerBase
    {
        private readonly IServerVoiceService _voiceService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerVoiceController(IServerVoiceService voiceService, IHubContext<PresenceHub> hubContext)
        {
            _voiceService = voiceService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet("server/{serverId:int}/voice")]
        public async Task<IActionResult> Get(int serverId)
        {
            return Ok(await _voiceService.GetAsync(UserId, serverId));
        }

        [HttpPost("server/{serverId:int}/channels/{channelId:int}/voice")]
        public async Task<IActionResult> Join(int serverId, int channelId, [FromBody] JoinServerVoiceDto model)
        {
            await NotifyAsync(await _voiceService.JoinAsync(UserId, serverId, channelId, model));
            return Ok();
        }

        [HttpDelete("voice")]
        public async Task<IActionResult> Leave()
        {
            await NotifyAsync(await _voiceService.LeaveAsync(UserId));
            return Ok();
        }

        [HttpPut("voice")]
        public async Task<IActionResult> Update([FromBody] UpdateServerVoiceDto model)
        {
            await NotifyAsync(await _voiceService.UpdateAsync(UserId, model));
            return Ok();
        }

        private async Task NotifyAsync(ServerVoiceChangeDto change)
        {
            foreach (var serverId in change.ServerIds.Distinct())
            {
                var memberIds = await _voiceService.GetMemberIdsAsync(serverId);
                await _hubContext.Clients.Users(memberIds).SendAsync("ServerVoiceChanged", new { serverId });
            }
        }
    }
}
