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
        public async Task<IActionResult> Invite(int id, [FromBody] InviteToLiveDto model, [FromServices] IGroupShareService groupShare, [FromServices] Discode_API_FinalProject.Helpers.GroupPublisher publisher, [FromServices] Microsoft.Extensions.Options.IOptions<Service.Helpers.Settings.AppSettings> app)
        {
            var invited = await _liveStreamService.InviteAsync(UserId, id, model.UserNames ?? new List<string>(), (model.GroupIds?.Count ?? 0) > 0);
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

            var groups = await groupShare.SendTextAsync(UserId, model.GroupIds, "📺 **" + invited.HostName + "** is live: **" + invited.Title + "**\n" + app.Value.ClientUrl.TrimEnd('/') + "/Social/Index?live=" + invited.StreamId);
            await publisher.PublishAsync(groups);
            return Ok(new { invited = invited.UserNames.Count, groups = groups.Select(x => x.GroupId).Distinct().Count() });
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _liveStreamService.GetActiveAsync(UserId, User.IsInRole(Domain.Constants.Roles.SuperAdmin) || User.IsInRole(Domain.Constants.Roles.Admin)));
        }
    }
}
