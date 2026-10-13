using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Friends;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> Send([FromBody] SendFriendRequestDto model)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _friendService.SendAsync(userId, model);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _friendService.GetAllAsync(userId);
            return Ok(result);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _friendService.GetPendingAsync(userId);
            return Ok(result);
        }

        [HttpDelete("request/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _friendService.CancelAsync(userId, id);
            return Ok();
        }

        [HttpPost("request/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _friendService.AcceptAsync(userId, id);
            return Ok();
        }

        [HttpPost("request/{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _friendService.DeclineAsync(userId, id);
            return Ok();
        }

        private string CurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
