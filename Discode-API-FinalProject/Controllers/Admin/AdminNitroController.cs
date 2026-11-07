using Discode_API_FinalProject.Helpers;
using Discode_API_FinalProject.Hubs;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Nitro;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/nitro")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminNitroController : ControllerBase
    {
        private readonly INitroService _nitro;
        private readonly IBoostService _boosts;
        private readonly IHubContext<PresenceHub> _hubContext;

        public AdminNitroController(INitroService nitro, IBoostService boosts, IHubContext<PresenceHub> hubContext)
        {
            _nitro = nitro;
            _boosts = boosts;
            _hubContext = hubContext;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // every admin can read who bought Nitro and what happened to it
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] bool activeOnly = false, [FromQuery] int page = 1)
        {
            return Ok(await _nitro.GetAdminPageAsync(search, activeOnly, page));
        }

        // giving and taking Nitro away by hand is for the super admin only
        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost("grant")]
        public async Task<IActionResult> Grant([FromBody] GrantNitroDto model)
        {
            await NitroNotifier.TellAsync(_hubContext, _boosts, await _nitro.GrantAsync(UserId, model));
            return Ok();
        }

        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] GrantNitroDto model)
        {
            await NitroNotifier.TellAsync(_hubContext, _boosts, await _nitro.RevokeAsync(UserId, model?.UserName ?? string.Empty));
            return Ok();
        }
    }
}
