using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.ServerChannels;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/server/{serverId:int}")]
    [ApiController]
    [Authorize]
    public class ServerChannelController : ControllerBase
    {
        private readonly IServerChannelService _channelService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public ServerChannelController(IServerChannelService channelService, IHubContext<PresenceHub> hubContext)
        {
            _channelService = channelService;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet("channels")]
        public async Task<IActionResult> GetAll(int serverId)
        {
            return Ok(await _channelService.GetAsync(UserId, serverId));
        }

        [HttpPost("channels")]
        public async Task<IActionResult> CreateChannel(int serverId, [FromBody] CreateServerChannelDto model)
        {
            var channel = await _channelService.CreateChannelAsync(UserId, serverId, model);
            await NotifyAsync(serverId);
            return Ok(channel);
        }

        [HttpPut("channels/{channelId:int}")]
        public async Task<IActionResult> UpdateChannel(int serverId, int channelId, [FromBody] UpdateServerChannelDto model)
        {
            var channel = await _channelService.UpdateChannelAsync(UserId, serverId, channelId, model);
            await NotifyAsync(serverId);
            return Ok(channel);
        }

        [HttpGet("channels/{channelId:int}/permissions")]
        public async Task<IActionResult> GetChannelPermissions(int serverId, int channelId)
        {
            return Ok(await _channelService.GetChannelPermissionsAsync(UserId, serverId, channelId));
        }

        [HttpGet("categories/{categoryId:int}/permissions")]
        public async Task<IActionResult> GetCategoryPermissions(int serverId, int categoryId)
        {
            return Ok(await _channelService.GetCategoryPermissionsAsync(UserId, serverId, categoryId));
        }

        [HttpGet("channels/{channelId:int}/viewers")]
        public async Task<IActionResult> GetViewers(int serverId, int channelId)
        {
            return Ok(await _channelService.GetViewerIdsAsync(UserId, serverId, channelId));
        }

        [HttpGet("permission-targets")]
        public async Task<IActionResult> SearchTargets(int serverId, [FromQuery] string? search)
        {
            return Ok(await _channelService.SearchTargetsAsync(UserId, serverId, search));
        }

        [HttpDelete("channels/{channelId:int}")]
        public async Task<IActionResult> DeleteChannel(int serverId, int channelId)
        {
            await _channelService.DeleteChannelAsync(UserId, serverId, channelId);
            await NotifyAsync(serverId);
            return Ok();
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory(int serverId, [FromBody] CreateServerCategoryDto model)
        {
            var category = await _channelService.CreateCategoryAsync(UserId, serverId, model);
            await NotifyAsync(serverId);
            return Ok(category);
        }

        [HttpPut("categories/{categoryId:int}")]
        public async Task<IActionResult> UpdateCategory(int serverId, int categoryId, [FromBody] UpdateServerCategoryDto model)
        {
            var category = await _channelService.UpdateCategoryAsync(UserId, serverId, categoryId, model);
            await NotifyAsync(serverId);
            return Ok(category);
        }

        [HttpDelete("categories/{categoryId:int}")]
        public async Task<IActionResult> DeleteCategory(int serverId, int categoryId)
        {
            await _channelService.DeleteCategoryAsync(UserId, serverId, categoryId);
            await NotifyAsync(serverId);
            return Ok();
        }

        private async Task NotifyAsync(int serverId)
        {
            var memberIds = await _channelService.GetMemberIdsAsync(serverId);
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerChannelsChanged", new { serverId });
            await _hubContext.Clients.Users(memberIds).SendAsync("ServerAuditChanged", new { serverId });
        }
    }
}
