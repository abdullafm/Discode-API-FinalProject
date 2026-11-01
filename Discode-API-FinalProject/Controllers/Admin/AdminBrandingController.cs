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
    [Route("api/admin/branding")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminBrandingController : ControllerBase
    {
        private readonly IBrandingService _branding;
        private readonly IHubContext<PresenceHub> _hub;
        private readonly IAdminChangeNotifier _changes;

        public AdminBrandingController(IBrandingService branding, IHubContext<PresenceHub> hub, IAdminChangeNotifier changes)
        {
            _branding = branding;
            _hub = hub;
            _changes = changes;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpPut("{slot}")]
        public async Task<IActionResult> Set(string slot, [FromBody] SetBrandingDto model)
        {
            return await DoneAsync(await _branding.SetAsync(UserId, slot, model));
        }

        [HttpDelete("{slot}")]
        public async Task<IActionResult> Reset(string slot)
        {
            return await DoneAsync(await _branding.ResetAsync(UserId, slot));
        }

        private async Task<IActionResult> DoneAsync(BrandingDto result)
        {
            await _hub.Clients.All.SendAsync("BrandingChanged", result);
            await _changes.NotifyAsync(new[] { "activity" }, Array.Empty<int>());
            return Ok(result);
        }
    }
}
