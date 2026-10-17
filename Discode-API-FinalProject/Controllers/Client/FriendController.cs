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
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public FriendController(IFriendService friendService, IHubContext<PresenceHub> hubContext)
        {
            _friendService = friendService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        private Task NotifyAsync(string otherUserId)
        {
            return _hubContext.Clients.Users(UserId, otherUserId).SendAsync("FriendsChanged");
        }

        [HttpPost("request")]
        public async Task<IActionResult> Send([FromBody] SendFriendRequestDto model)
        {
            var result = await _friendService.SendAsync(UserId, model);
            await NotifyAsync(result.UserId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _friendService.GetAllAsync(UserId));
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(await _friendService.GetPendingAsync(UserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remove(int id)
        {
            var otherUserId = await _friendService.RemoveAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }

        [HttpDelete("request/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var otherUserId = await _friendService.CancelAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }

        [HttpPost("request/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var otherUserId = await _friendService.AcceptAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }

        [HttpPost("request/{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            var otherUserId = await _friendService.DeclineAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }
    }
}
