using Discode_API_FinalProject.Hubs;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Repository.Notifications;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/content")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminContentController : ControllerBase
    {
        private readonly IAdminContentService _content;
        private readonly IUserModerationService _moderation;
        private readonly IHubContext<PresenceHub> _hub;
        private readonly SystemMessenger _messenger;
        private readonly IAdminChangeNotifier _changes;
        private readonly Filters.AccountStatusCache _statusCache;

        public AdminContentController(IAdminContentService content, IUserModerationService moderation, IHubContext<PresenceHub> hub, SystemMessenger messenger, IAdminChangeNotifier changes, Filters.AccountStatusCache statusCache)
        {
            _content = content;
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
        public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(await _content.GetAsync(type, search, page, pageSize));
        }

        [HttpPost("{type}/{id:int}/warn")]
        public async Task<IActionResult> Warn(string type, int id, [FromBody] LiveReasonDto model)
        {
            var found = await _content.FindAsync(type, id);
            var result = await _moderation.WarnAsync(Actor, found.AuthorId, new WarnUserDto { Reason = "Your " + found.Kind + (found.Snippet.Length > 0 ? " \"" + found.Snippet + "\"" : string.Empty) + ": " + (model.Reason ?? string.Empty).Trim() });
            await _hub.Clients.User(found.AuthorId).SendAsync("AccountWarning", new[] { new { id = result.WarningId, reason = result.Reason, count = result.WarningCount } });
            await _messenger.SendAsync(found.AuthorId, $"You received a warning from the Discode team about your {found.Kind}.\n\nReason: {result.Reason}\n\nPlease follow the community rules. Further violations can lead to removal of content, a timeout or a ban.");
            _statusCache.Invalidate(found.AuthorId);
            await _changes.NotifyAsync(new[] { "users", "activity" }, Array.Empty<int>());
            return Ok();
        }

        [HttpPost("{type}/{id:int}/delete")]
        public async Task<IActionResult> Delete(string type, int id, [FromBody] LiveReasonDto model)
        {
            var reason = (model.Reason ?? string.Empty).Trim();
            var removed = await _content.DeleteAsync(Actor, type, id, reason);

            if (removed.Kind == "story")
            {
                await _hub.Clients.All.SendAsync("StoriesChanged");
            }
            else
            {
                await _hub.Clients.All.SendAsync("PostsChanged");
            }

            var what = removed.Snippet.Length > 0 ? $"Your {removed.Kind} \"{removed.Snippet}\"" : $"Your {removed.Kind}";
            await _messenger.SendAsync(removed.AuthorId, $"{what} was removed by the Discode team.\n\nReason: {reason}\n\nPlease follow the community rules.");
            await _changes.NotifyAsync(new[] { "content", "reports", "activity" }, Array.Empty<int>());
            return Ok();
        }
    }
}
