using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Posts;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public PostController(IPostService postService, IHubContext<PresenceHub> hubContext)
        {
            _postService = postService;
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
        public async Task<IActionResult> Create([FromBody] CreatePostDto model)
        {
            var result = await _postService.CreateAsync(UserId, model);
            await _hubContext.Clients.All.SendAsync("PostsChanged");
            return Ok(result);
        }

        [HttpGet("explore")]
        public async Task<IActionResult> Explore()
        {
            return Ok(await _postService.GetExploreAsync(UserId));
        }

        [HttpGet("friends")]
        public async Task<IActionResult> Friends()
        {
            return Ok(await _postService.GetFriendsAsync(UserId));
        }

        [HttpGet("user/{userName}")]
        public async Task<IActionResult> ByUser(string userName)
        {
            return Ok(await _postService.GetByUserAsync(UserId, userName));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _postService.DeleteAsync(UserId, id);
            await _hubContext.Clients.All.SendAsync("PostsChanged");
            return Ok();
        }
    }
}
