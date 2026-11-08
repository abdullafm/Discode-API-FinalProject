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
        private readonly ICallService _callService;
        private readonly ILiveStreamService _liveStreamService;

        public BlockController(IUserBlockService userBlockService, IHubContext<PresenceHub> hubContext, ICallService callService, ILiveStreamService liveStreamService)
        {
            _userBlockService = userBlockService;
            _hubContext = hubContext;
            _callService = callService;
            _liveStreamService = liveStreamService;
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
            foreach (var ended in await _callService.EndIfNotAllowedAsync(UserId, result.UserId))
            {
                await PresenceHub.AnnounceCallEndAsync(_hubContext.Clients, ended);
            }

            // nobody stays in the live stream of someone who is blocked either way
            foreach (var left in await _liveStreamService.EjectBlockedAsync(UserId, result.UserId))
            {
                await _hubContext.Groups.RemoveFromGroupAsync(left.ConnectionId, PresenceHub.LiveGroup(left.StreamId));
                await _hubContext.Clients.Client(left.ConnectionId).SendAsync("LiveEnded", left.StreamId);
                await _hubContext.Clients.Group(PresenceHub.LiveGroup(left.StreamId)).SendAsync("ViewerLeft", left.StreamId, left.ConnectionId);
                await _hubContext.Clients.All.SendAsync("LiveViewers", left.StreamId, left.ViewerCount);
            }

            await _hubContext.Clients.Users(UserId, result.UserId).SendAsync("FriendsChanged");
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(UserId)).SendAsync("ProfileChanged", new { userId = UserId });
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(result.UserId)).SendAsync("ProfileChanged", new { userId = result.UserId });
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
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(UserId)).SendAsync("ProfileChanged", new { userId = UserId });
            await _hubContext.Clients.Group(PresenceHub.ProfileGroup(result.UserId)).SendAsync("ProfileChanged", new { userId = result.UserId });
            return Ok();
        }
    }
}
