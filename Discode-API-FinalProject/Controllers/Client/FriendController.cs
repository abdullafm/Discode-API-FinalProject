using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public FriendController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpPost("request")]
        public async Task<IActionResult> Send([FromBody] SendFriendRequestDto model)
        {
            return Ok(await _friendService.SendAsync(UserId, model));
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
            await _friendService.RemoveAsync(UserId, id);
            return Ok();
        }

        [HttpDelete("request/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _friendService.CancelAsync(UserId, id);
            return Ok();
        }

        [HttpPost("request/{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            await _friendService.AcceptAsync(UserId, id);
            return Ok();
        }

        [HttpPost("request/{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            await _friendService.DeclineAsync(UserId, id);
            return Ok();
        }
    }
}
