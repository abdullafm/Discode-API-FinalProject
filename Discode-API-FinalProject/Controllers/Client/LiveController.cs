using Microsoft.AspNetCore.Authorization;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

        public LiveController(ILiveStreamService liveStreamService, IHubContext<PresenceHub> hubContext)
        {
            _liveStreamService = liveStreamService;
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
            await _hubContext.Clients.Users(invited).SendAsync("NotificationsChanged");
            return Ok(new { invited = invited.Count });
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _liveStreamService.GetActiveAsync(UserId));
        }
    }
}
