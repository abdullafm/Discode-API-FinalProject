using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Friends;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IgnoreController : ControllerBase
    {
        private readonly IUserIgnoreService _userIgnoreService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public IgnoreController(IUserIgnoreService userIgnoreService, IHubContext<PresenceHub> hubContext)
        {
            _userIgnoreService = userIgnoreService;
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
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _userIgnoreService.GetIgnoredUserNamesAsync(UserId));
        }

        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] IgnoreUserDto model)
        {
            var result = await _userIgnoreService.ToggleAsync(UserId, model);
            await _hubContext.Clients.User(UserId).SendAsync("IgnoreChanged", result);
            return Ok(result);
        }
    }
}
