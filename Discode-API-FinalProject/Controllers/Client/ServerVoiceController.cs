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
        public async Task<IActionResult> Join(int serverId, int channelId, [FromBody] JoinServerVoiceDto model, [FromServices] Discode_API_FinalProject.Helpers.VoiceSessions sessions)
        {
            await NotifyAsync(await _voiceService.JoinAsync(UserId, serverId, channelId, model));
            await sessions.LeaveOthersAsync(UserId, Discode_API_FinalProject.Helpers.VoiceKind.ServerChannel);
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

        [HttpPut("server/{serverId:int}/voice/{targetUserId}/moderation")]
        public async Task<IActionResult> Moderate(int serverId, string targetUserId, [FromBody] ModerateServerVoiceDto model)
        {
            await TellAsync(serverId, await _voiceService.ModerateAsync(UserId, serverId, targetUserId, model ?? new ModerateServerVoiceDto()));
            return Ok();
        }

        [HttpDelete("server/{serverId:int}/voice/{targetUserId}")]
        public async Task<IActionResult> Disconnect(int serverId, string targetUserId)
        {
            await TellAsync(serverId, await _voiceService.DisconnectAsync(UserId, serverId, targetUserId));
            return Ok();
        }

        private async Task TellAsync(int serverId, ServerVoiceModerationDto result)
        {
            await NotifyAsync(result.Change);
            if (result.Kind.Length > 0)
            {
                await _hubContext.Clients.User(result.TargetUserId).SendAsync("ServerVoiceModerated", new { serverId, kind = result.Kind });
            }
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
