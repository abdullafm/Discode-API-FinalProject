using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Stories;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StoryController : ControllerBase
    {
        private readonly IStoryService _storyService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public StoryController(IStoryService storyService, IHubContext<PresenceHub> hubContext)
        {
            _storyService = storyService;
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
        public async Task<IActionResult> Create([FromBody] CreateStoryDto model)
        {
            var result = await _storyService.CreateAsync(UserId, model);
            await _hubContext.Clients.All.SendAsync("StoriesChanged");
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _storyService.GetAsync(UserId));
        }

        [HttpPost("{id:int}/like")]
        public async Task<IActionResult> Like(int id)
        {
            var result = await _storyService.ToggleLikeAsync(UserId, id);
            await _hubContext.Clients.All.SendAsync("StoriesChanged");
            return Ok(result);
        }

        [HttpPost("{id:int}/view")]
        public async Task<IActionResult> View(int id)
        {
            var authorId = await _storyService.RecordViewAsync(UserId, id);
            if (authorId is not null)
            {
                await _hubContext.Clients.User(authorId).SendAsync("StoryViewsChanged", id);
            }

            return Ok();
        }

        [HttpGet("{id:int}/viewers")]
        public async Task<IActionResult> Viewers(int id)
        {
            return Ok(await _storyService.GetViewersAsync(UserId, id));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _storyService.DeleteAsync(UserId, id);
            await _hubContext.Clients.All.SendAsync("StoriesChanged");
            return Ok();
        }
    }
}
