using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Groups;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public GroupController(IGroupService groupService, IHubContext<PresenceHub> hubContext)
        {
            _groupService = groupService;
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
        public async Task<IActionResult> Create([FromBody] CreateGroupDto model)
        {
            var group = await _groupService.CreateAsync(UserId, model);
            if (!group.Existing)
            {
                await _hubContext.Clients.Users(group.Members.Select(m => m.UserId)).SendAsync("GroupsChanged");
            }

            return Ok(group);
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _groupService.GetMineAsync(UserId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await _groupService.GetAsync(UserId, id));
        }
    }
}
