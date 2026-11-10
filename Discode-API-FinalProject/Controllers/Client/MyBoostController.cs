using Discode_API_FinalProject.Helpers;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Nitro;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/boost")]
    [ApiController]
    [Authorize]
    public class MyBoostController : ControllerBase
    {
        private readonly IBoostService _boosts;
        private readonly IHubContext<PresenceHub> _hubContext;

        public MyBoostController(IBoostService boosts, IHubContext<PresenceHub> hubContext)
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

        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            return Ok(await _boosts.GetMineAsync(UserId));
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MoveBoostDto model)
        {
            var result = await _boosts.MoveAsync(UserId, model);
            await NitroNotifier.TellAsync(_hubContext, _boosts, result.Change);
            return Ok(await _boosts.GetMineAsync(UserId));
        }
    }
}
