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
        private readonly IBlockService _blockService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public BlockController(IBlockService blockService, IHubContext<PresenceHub> hubContext)
        {
            _blockService = blockService;
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
            var result = await _blockService.BlockAsync(UserId, model);
            await _hubContext.Clients.Users(UserId, result.UserId).SendAsync("FriendsChanged");
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _blockService.GetAllAsync(UserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Unblock(int id)
        {
            await _blockService.UnblockAsync(UserId, id);
            return Ok();
        }
    }
}
