using Microsoft.AspNetCore.Authorization;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Live;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LiveController : ControllerBase
    {
        private readonly ILiveStreamService _liveStreamService;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly IDirectMessageService _directMessages;

        public LiveController(ILiveStreamService liveStreamService, IDirectMessageService directMessages, IHubContext<PresenceHub> hubContext)
        {
            _liveStreamService = liveStreamService;
            _directMessages = directMessages;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpPost("{id:int}/invite")]
        public async Task<IActionResult> Invite(int id, [FromBody] InviteToLiveDto model)
        {
            var invited = await _liveStreamService.InviteAsync(UserId, id, model.UserNames ?? new List<string>());
            var share = new LiveShareDto
            {
                StreamId = invited.StreamId,
                Title = invited.Title,
                HostName = invited.HostName,
                ImageUrl = invited.ThumbnailUrl
            };
            foreach (var userName in invited.UserNames)
            {
                var sent = await _directMessages.SendLiveShareAsync(UserId, userName, share);
                await _hubContext.Clients.User(sent.SenderId).SendAsync("DirectMessage", new
                {
                    message = sent.MessageForSender,
                    conversation = sent.ConversationForSender
                });
                if (!sent.MessageForSender.Undelivered)
                {
                    await _hubContext.Clients.User(sent.RecipientId).SendAsync("DirectMessage", new
                    {
                        message = sent.MessageForRecipient,
                        conversation = sent.ConversationForRecipient
                    });
                }
            }

            return Ok(new { invited = invited.UserNames.Count });
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _liveStreamService.GetActiveAsync(UserId));
        }
    }
}
