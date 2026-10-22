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
        private readonly IGroupCallService _groupCallService;
        private readonly IHubContext<PresenceHub> _hubContext;

        public GroupController(IGroupService groupService, IGroupCallService groupCallService, IHubContext<PresenceHub> hubContext)
        {
            _groupService = groupService;
            _groupCallService = groupCallService;
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

        [HttpGet("{id:int}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            return Ok(await _groupService.GetMessagesAsync(UserId, id));
        }

        [HttpPost("{id:int}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendGroupMessageDto model)
        {
            var change = await _groupService.SendMessageAsync(UserId, id, model);
            await PublishAsync(change);
            return Ok(change.Message);
        }

        [HttpPost("{id:int}/polls")]
        public async Task<IActionResult> SendPoll(int id, [FromBody] Service.Helpers.DTOs.DirectMessages.CreatePollDto model)
        {
            var change = await _groupService.SendPollAsync(UserId, id, model);
            await PublishAsync(change);
            return Ok(change.Message);
        }

        [HttpDelete("{id:int}/messages/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(int id, int messageId)
        {
            var deleted = await _groupService.DeleteMessageAsync(UserId, id, messageId);
            await _hubContext.Clients.Users(deleted.UserIds).SendAsync("GroupMessageDeleted", new { groupId = deleted.GroupId, messageId = deleted.MessageId });
            return NoContent();
        }

        [HttpPost("{id:int}/messages/{messageId:int}/forward")]
        public async Task<IActionResult> Forward(int id, int messageId, [FromBody] Service.Helpers.DTOs.DirectMessages.ForwardMessageDto model)
        {
            var sent = await _groupService.ForwardMessageAsync(UserId, id, messageId, model);
            foreach (var item in sent)
            {
                await _hubContext.Clients.User(item.SenderId).SendAsync("DirectMessage", new { message = item.MessageForSender, conversation = item.ConversationForSender });
                await _hubContext.Clients.User(item.RecipientId).SendAsync("DirectMessage", new { message = item.MessageForRecipient, conversation = item.ConversationForRecipient });
            }

            return Ok(new { forwarded = model.UserNames.Count });
        }

        [HttpPut("{id:int}/messages/{messageId:int}/reactions")]
        public async Task<IActionResult> React(int id, int messageId, [FromBody] Service.Helpers.DTOs.DirectMessages.ToggleReactionDto model)
        {
            var update = await _groupService.ToggleReactionAsync(UserId, id, messageId, model);
            await PublishUpdateAsync(update);
            return Ok(update.Views[UserId]);
        }

        [HttpPut("{id:int}/messages/{messageId:int}/poll-votes")]
        public async Task<IActionResult> VotePoll(int id, int messageId, [FromBody] Service.Helpers.DTOs.DirectMessages.VotePollDto model)
        {
            var update = await _groupService.VotePollAsync(UserId, id, messageId, model);
            await PublishUpdateAsync(update);
            return Ok(update.Views[UserId]);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGroupDto model)
        {
            var change = await _groupService.UpdateAsync(UserId, id, model);
            await PublishAsync(change);
            return Ok(change.Group);
        }

        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMembers(int id, [FromBody] AddGroupMembersDto model)
        {
            var change = await _groupService.AddMembersAsync(UserId, id, model);
            await PublishAsync(change);
            return Ok(change.Group);
        }

        [HttpDelete("{id:int}/members/{userName}")]
        public async Task<IActionResult> RemoveMember(int id, string userName)
        {
            var change = await _groupService.RemoveMemberAsync(UserId, id, userName);
            await PublishAsync(change);
            return Ok(change.Group);
        }

        [HttpPost("{id:int}/leave")]
        public async Task<IActionResult> Leave(int id)
        {
            foreach (var callChange in await _groupCallService.LeaveAllForAsync(UserId))
            {
                await PresenceHub.AnnounceGroupCallAsync(_hubContext.Clients, callChange);
            }

            var change = await _groupService.LeaveAsync(UserId, id);
            await PublishAsync(change);
            return Ok(new { left = true });
        }

        private async Task PublishUpdateAsync(GroupMessageUpdateDto update)
        {
            foreach (var view in update.Views)
            {
                await _hubContext.Clients.User(view.Key).SendAsync("GroupMessageUpdated", new { groupId = update.GroupId, message = view.Value });
            }
        }

        private async Task PublishAsync(GroupChangeDto change)
        {
            var users = _hubContext.Clients.Users(change.UserIds);
            await users.SendAsync("GroupsChanged");
            await users.SendAsync("GroupUpdated", new { groupId = change.GroupId, group = change.Group });
            if (change.Message is not null)
            {
                await users.SendAsync("GroupMessage", new { groupId = change.GroupId, message = change.Message });
            }
        }
    }
}
