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
        private readonly ICommentService _commentService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public PostController(IPostService postService, ICommentService commentService, IHubContext<PresenceHub> hubContext)
        {
            _postService = postService;
            _commentService = commentService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        private async Task NotifyAsync()
        {
            await _hubContext.Clients.All.SendAsync("PostsChanged");
            await _hubContext.Clients.All.SendAsync("NotificationsChanged");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostDto model)
        {
            var result = await _postService.CreateAsync(UserId, model);
            await NotifyAsync();
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
            await NotifyAsync();
            return Ok();
        }

        [HttpPost("{id:int}/like")]
        public async Task<IActionResult> Like(int id)
        {
            var result = await _postService.ToggleLikeAsync(UserId, id);
            await NotifyAsync();
            return Ok(result);
        }

        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> Comment(int id, [FromBody] CreateCommentDto model)
        {
            var result = await _commentService.CreateAsync(UserId, id, model);
            await NotifyAsync();
            return Ok(result);
        }

        [HttpDelete("comments/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            await _commentService.DeleteAsync(UserId, commentId);
            await NotifyAsync();
            return Ok();
        }

        [HttpPost("comments/{commentId:int}/like")]
        public async Task<IActionResult> LikeComment(int commentId)
        {
            var result = await _commentService.ToggleLikeAsync(UserId, commentId);
            await NotifyAsync();
            return Ok(result);
        }
    }
}
