using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.ServerMessages;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/server/{serverId:int}/channels/{channelId:int}/messages")]
    [ApiController]
    [Authorize]
    public class ServerMessageController : ControllerBase
    {
        private readonly IServerMessageService _messageService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerMessageController(IServerMessageService messageService, IHubContext<PresenceHub> hubContext)
        {
            _messageService = messageService;
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
        public async Task<IActionResult> GetMessages(int serverId, int channelId, [FromQuery] int? before = null)
        {
            return Ok(await _messageService.GetMessagesAsync(UserId, serverId, channelId, before));
        }

        [HttpPost]
        public async Task<IActionResult> Send(int serverId, int channelId, [FromBody] SendServerMessageDto model)
        {
            var change = await _messageService.SendAsync(UserId, serverId, channelId, model);
            await _hubContext.Clients.Users(change.ViewerIds).SendAsync("ServerMessage", new { serverId, channelId, message = change.Message });
            return Ok(change.Message);
        }

        [HttpDelete("{messageId:int}")]
        public async Task<IActionResult> Delete(int serverId, int channelId, int messageId)
        {
            var deleted = await _messageService.DeleteAsync(UserId, serverId, channelId, messageId);
            await _hubContext.Clients.Users(deleted.ViewerIds).SendAsync("ServerMessageDeleted", new { serverId, channelId, messageId });
            return NoContent();
        }

        [HttpPut("{messageId:int}/reactions")]
        public async Task<IActionResult> React(int serverId, int channelId, int messageId, [FromBody] ToggleReactionDto model)
        {
            var update = await _messageService.ToggleReactionAsync(UserId, serverId, channelId, messageId, model);
            foreach (var view in update.Views)
            {
                await _hubContext.Clients.User(view.Key).SendAsync("ServerMessageUpdated", new { serverId, channelId, message = view.Value });
            }

            return Ok(update.Views.TryGetValue(UserId, out var mine) ? mine : null);
        }
    }
}
