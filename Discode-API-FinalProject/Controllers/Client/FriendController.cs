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
        private readonly IFriendRequestService _friendRequestService;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly ICallService _callService;

        public FriendController(IFriendRequestService friendRequestService, IHubContext<PresenceHub> hubContext, ICallService callService)
        {
            _friendRequestService = friendRequestService;
            _hubContext = hubContext;
            _callService = callService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        private async Task NotifyAsync(string otherUserId)
        {
            await _hubContext.Clients.Users(UserId, otherUserId).SendAsync("FriendsChanged");
            await _hubContext.Clients.Users(UserId, otherUserId).SendAsync("NotificationsChanged");
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(UserId)).SendAsync("ProfileChanged", new { userId = UserId });
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(otherUserId)).SendAsync("ProfileChanged", new { userId = otherUserId });
        }

        [HttpPost("request")]
        public async Task<IActionResult> Send([FromBody] SendFriendRequestDto model)
        {
            var result = await _friendRequestService.SendAsync(UserId, model);
            await NotifyAsync(result.UserId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _friendRequestService.GetAllAsync(UserId));
        }

        [HttpGet("user/{userName}")]
        public async Task<IActionResult> ByUser(string userName)
        {
            return Ok(await _friendRequestService.GetUserFriendsAsync(UserId, userName));
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(await _friendRequestService.GetPendingAsync(UserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remove(int id)
        {
            var otherUserId = await _friendRequestService.RemoveAsync(UserId, id);
            await NotifyAsync(otherUserId);
            foreach (var ended in await _callService.EndIfNotAllowedAsync(UserId, otherUserId))
            {
                await PresenceHub.AnnounceCallEndAsync(_hubContext.Clients, ended);
            }
            return Ok();
        }

        [HttpDelete("request/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var otherUserId = await _friendRequestService.CancelAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }

        [HttpPost("request/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var otherUserId = await _friendRequestService.AcceptAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }

        [HttpPost("request/{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            var otherUserId = await _friendRequestService.DeclineAsync(UserId, id);
            await NotifyAsync(otherUserId);
            return Ok();
        }
    }
}
