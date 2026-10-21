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
    public class BlockController : ControllerBase
    {
        private readonly IUserBlockService _userBlockService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public BlockController(IUserBlockService userBlockService, IHubContext<PresenceHub> hubContext)
        {
            _userBlockService = userBlockService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Block([FromBody] BlockUserDto model)
        {
            var result = await _userBlockService.BlockAsync(UserId, model);
            await _hubContext.Clients.Users(UserId, result.UserId).SendAsync("FriendsChanged");
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _userBlockService.GetAllAsync(UserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Unblock(int id)
        {
            var result = await _userBlockService.UnblockAsync(UserId, id);
            await _hubContext.Clients.Users(UserId, result.UserId).SendAsync("FriendsChanged");
            return Ok();
        }
    }
}
