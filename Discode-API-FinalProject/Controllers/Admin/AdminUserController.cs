using Domain.Constants;
using Discode_API_FinalProject.Filters;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Repository.Notifications;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserModerationService _moderation;
        private readonly IHubContext<PresenceHub> _hub;
        private readonly IAdminChangeNotifier _changes;
        private readonly AccountStatusCache _statusCache;
        private readonly SystemMessenger _messenger;

        public AdminUserController(IUserModerationService moderation, IHubContext<PresenceHub> hub, IAdminChangeNotifier changes, AccountStatusCache statusCache, SystemMessenger messenger)
        {
            _messenger = messenger;
            _moderation = moderation;
            _hub = hub;
            _changes = changes;
            _statusCache = statusCache;
        }

        private ModerationActor Actor => new()
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            IsSuperAdmin = User.IsInRole(Roles.SuperAdmin)
        };

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            return Ok(await _moderation.GetAsync(Actor, id));
        }

        [HttpPost("{id}/warn")]
        public async Task<IActionResult> Warn(string id, [FromBody] WarnUserDto model)
        {
            var result = await _moderation.WarnAsync(Actor, id, model);
            await _hub.Clients.User(id).SendAsync("AccountWarning", new[] { new { id = result.WarningId, reason = result.Reason, count = result.WarningCount } });
            await _messenger.SendAsync(id, $"You received a warning from the Discode team.\n\nReason: {result.Reason}\n\nPlease follow the community rules. Further violations can lead to a timeout or a ban.");
            return await DoneAsync(result);
        }

        [HttpPost("{id}/timeout")]
        public async Task<IActionResult> Timeout(string id, [FromBody] TimeoutUserDto model)
        {
            var result = await _moderation.TimeoutAsync(Actor, id, model);
            await _hub.Clients.User(id).SendAsync("AccountTimeout", new { seconds = (int)Math.Ceiling((result.TimeoutUntil!.Value - DateTime.UtcNow).TotalSeconds), until = result.TimeoutUntil, reason = result.Reason });
            return await DoneAsync(result);
        }

        [HttpPost("{id}/timeout/clear")]
        public async Task<IActionResult> ClearTimeout(string id)
        {
            var result = await _moderation.ClearTimeoutAsync(Actor, id);
            await _hub.Clients.User(id).SendAsync("AccountTimeout", new { seconds = 0, until = (DateTime?)null, reason = string.Empty });
            return await DoneAsync(result);
        }

        [HttpPost("{id}/role")]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleDto model)
        {
            return await DoneAsync(await _moderation.ChangeRoleAsync(Actor, id, model));
        }

        [HttpPost("{id}/live-ban")]
        public async Task<IActionResult> LiveBan(string id, [FromBody] LiveBanDto model, [FromServices] ILiveStreamService live)
        {
            var result = await _moderation.LiveBanAsync(Actor, id, model);
            var ended = (await live.StopAsync(id)).ToList();
            foreach (var streamId in ended)
            {
                await _hub.Clients.Group("live-" + streamId).SendAsync("LiveEnded", streamId);
            }

            if (ended.Count > 0)
            {
                await _hub.Clients.All.SendAsync("LiveChanged");
            }

            var when = result.TimeoutUntil is null ? "permanently" : "until " + result.TimeoutUntil.Value.ToString("yyyy-MM-dd HH:mm") + " UTC";
            await _messenger.SendAsync(id, $"You can no longer go live {when}.\n\nReason: {result.Reason}\n\nThe rest of Discode still works as usual.");
            return await DoneAsync(result);
        }

        [HttpPost("{id}/live-unban")]
        public async Task<IActionResult> LiveUnban(string id)
        {
            var result = await _moderation.LiveUnbanAsync(Actor, id);
            await _messenger.SendAsync(id, "Your live ban was removed. You can go live again.");
            return await DoneAsync(result);
        }

        [HttpPost("{id}/kick")]
        public async Task<IActionResult> Kick(string id, [FromBody] KickUserDto model)
        {
            return await DoneAsync(await _moderation.KickAsync(Actor, id, model));
        }

        [HttpPost("{id}/ban")]
        public async Task<IActionResult> Ban(string id, [FromBody] BanUserDto model)
        {
            return await DoneAsync(await _moderation.BanAsync(Actor, id, model));
        }

        [HttpPost("{id}/unban")]
        public async Task<IActionResult> Unban(string id)
        {
            return await DoneAsync(await _moderation.UnbanAsync(Actor, id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromBody] DeleteUserDto model)
        {
            return await DoneAsync(await _moderation.DeleteAsync(Actor, id, model));
        }

        private async Task<IActionResult> DoneAsync(ModerationResultDto result)
        {
            _statusCache.Invalidate(result.UserId);
            if (result.ForceLogout)
            {
                await _hub.Clients.User(result.UserId).SendAsync("ForceLogout", new { reason = result.Reason });
            }

            await _changes.NotifyAsync(new[] { "users", "activity" }, Array.Empty<int>());
            return Ok(result);
        }
    }
}
