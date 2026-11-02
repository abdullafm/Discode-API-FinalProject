using Discode_API_FinalProject.Hubs;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Repository.Notifications;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/live")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminLiveController : ControllerBase
    {
        private readonly ILiveStreamService _live;
        private readonly IUserModerationService _moderation;
        private readonly IHubContext<PresenceHub> _hub;
        private readonly SystemMessenger _messenger;
        private readonly IAdminChangeNotifier _changes;
        private readonly Filters.AccountStatusCache _statusCache;

        public AdminLiveController(ILiveStreamService live, IUserModerationService moderation, IHubContext<PresenceHub> hub, SystemMessenger messenger, IAdminChangeNotifier changes, Filters.AccountStatusCache statusCache)
        {
            _live = live;
            _moderation = moderation;
            _hub = hub;
            _messenger = messenger;
            _changes = changes;
            _statusCache = statusCache;
        }

        private ModerationActor Actor => new()
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            IsSuperAdmin = User.IsInRole(Roles.SuperAdmin)
        };

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(new AdminLiveListDto { Items = await _live.GetActiveForAdminAsync() });
        }

        [HttpPost("{id:int}/warn")]
        public async Task<IActionResult> Warn(int id, [FromBody] LiveReasonDto model)
        {
            var stream = await FindAsync(id);
            var result = await _moderation.WarnAsync(Actor, stream.HostId, new WarnUserDto { Reason = "Your live stream \"" + stream.Title + "\": " + (model.Reason ?? string.Empty).Trim() });
            await _hub.Clients.User(stream.HostId).SendAsync("AccountWarning", new[] { new { id = result.WarningId, reason = result.Reason, count = result.WarningCount } });
            await _messenger.SendAsync(stream.HostId, $"You received a warning from the Discode team about your live stream.\n\nReason: {result.Reason}\n\nPlease follow the community rules. Further violations can end your stream or stop you from going live.");
            _statusCache.Invalidate(stream.HostId);
            await _changes.NotifyAsync(new[] { "users", "activity" }, Array.Empty<int>());
            return Ok();
        }

        [HttpPost("{id:int}/end")]
        public async Task<IActionResult> End(int id, [FromBody] LiveReasonDto model)
        {
            var stream = await FindAsync(id);
            await _moderation.LogLiveEndAsync(Actor, stream.HostId, model.Reason ?? string.Empty, stream.Title);

            var ended = (await _live.StopAsync(stream.HostId)).ToList();
            foreach (var streamId in ended)
            {
                await _hub.Clients.Group("live-" + streamId).SendAsync("LiveEnded", streamId);
            }

            await _hub.Clients.All.SendAsync("LiveChanged");
            var reason = (model.Reason ?? string.Empty).Trim();
            await _hub.Clients.User(stream.HostId).SendAsync("LiveClosedByAdmin", new { title = stream.Title, reason });
            await _messenger.SendAsync(stream.HostId, $"Your live stream \"{stream.Title}\" was ended by the Discode team.\n\nReason: {reason}");
            await _changes.NotifyAsync(new[] { "live", "activity" }, Array.Empty<int>());
            return Ok();
        }

        private async Task<AdminLiveStreamDto> FindAsync(int id)
        {
            var streams = await _live.GetActiveForAdminAsync();
            return streams.FirstOrDefault(x => x.Id == id) ?? throw new NotFoundException("That stream is not live anymore.");
        }
    }
}
