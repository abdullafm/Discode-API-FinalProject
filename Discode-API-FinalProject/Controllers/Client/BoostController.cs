using Discode_API_FinalProject.Helpers;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/server/{serverId:int}/boost")]
    [ApiController]
    [Authorize]
    public class BoostController : ControllerBase
    {
        private readonly IBoostService _boosts;
        private readonly IHubContext<PresenceHub> _hubContext;

        public BoostController(IBoostService boosts, IHubContext<PresenceHub> hubContext)
        {
            _boosts = boosts;
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
        public async Task<IActionResult> Get(int serverId)
        {
            return Ok(await _boosts.GetInfoAsync(UserId, serverId));
        }

        [HttpPost]
        public async Task<IActionResult> Boost(int serverId)
        {
            var result = await _boosts.BoostAsync(UserId, serverId);
            await NitroNotifier.TellAsync(_hubContext, _boosts, result.Change);
            return Ok(result.Info);
        }

        [HttpDelete]
        public async Task<IActionResult> Unboost(int serverId)
        {
            var result = await _boosts.UnboostAsync(UserId, serverId);
            await NitroNotifier.TellAsync(_hubContext, _boosts, result.Change);
            return Ok(result.Info);
        }
    }
}
