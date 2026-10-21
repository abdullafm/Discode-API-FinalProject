using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.DirectMessages;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/dm")]
    [ApiController]
    [Authorize]
    public class DirectMessageController : ControllerBase
    {
        private readonly IDirectMessageService _directMessages;
        private readonly IHubContext<PresenceHub> _hubContext;

        public DirectMessageController(IDirectMessageService directMessages, IHubContext<PresenceHub> hubContext)
        {
            _directMessages = directMessages;
            _hubContext = hubContext;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            return Ok(await _directMessages.GetConversationsAsync(UserId));
        }

        [HttpGet("{userName}")]
        public async Task<IActionResult> GetChat(string userName)
        {
            return Ok(await _directMessages.GetChatAsync(UserId, userName));
        }

        [HttpGet("{userName}/relation")]
        public async Task<IActionResult> GetRelation(string userName)
        {
            return Ok(await _directMessages.GetRelationAsync(UserId, userName));
        }

        [HttpPost("{userName}")]
        public async Task<IActionResult> Send(string userName, [FromBody] SendMessageDto model)
        {
            var sent = await _directMessages.SendAsync(UserId, userName, model);

            await _hubContext.Clients.User(sent.SenderId).SendAsync("DirectMessage", new
            {
                message = sent.MessageForSender,
                conversation = sent.ConversationForSender
            });
            await _hubContext.Clients.User(sent.RecipientId).SendAsync("DirectMessage", new
            {
                message = sent.MessageForRecipient,
                conversation = sent.ConversationForRecipient
            });

            return Ok(sent.MessageForSender);
        }

        [HttpPut("messages/{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] SendMessageDto model)
        {
            var edited = await _directMessages.EditAsync(UserId, id, model);

            await _hubContext.Clients.User(edited.SenderId).SendAsync("DirectMessageEdited", new { message = edited.MessageForSender });
            await _hubContext.Clients.User(edited.RecipientId).SendAsync("DirectMessageEdited", new { message = edited.MessageForRecipient });

            return Ok(edited.MessageForSender);
        }

        [HttpPost("messages/{id:int}/forward")]
        public async Task<IActionResult> Forward(int id, [FromBody] ForwardMessageDto model)
        {
            var sent = await _directMessages.ForwardAsync(UserId, id, model);

            foreach (var item in sent)
            {
                await _hubContext.Clients.User(item.SenderId).SendAsync("DirectMessage", new { message = item.MessageForSender, conversation = item.ConversationForSender });
                await _hubContext.Clients.User(item.RecipientId).SendAsync("DirectMessage", new { message = item.MessageForRecipient, conversation = item.ConversationForRecipient });
            }

            return Ok(new { forwarded = model.UserNames.Count });
        }

        [HttpDelete("messages/{id:int}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var deleted = await _directMessages.DeleteAsync(UserId, id);

            var payload = new { messageId = deleted.MessageId, conversationId = deleted.ConversationId, preview = deleted.Preview };
            await _hubContext.Clients.User(deleted.SenderId).SendAsync("DirectMessageDeleted", payload);
            await _hubContext.Clients.User(deleted.RecipientId).SendAsync("DirectMessageDeleted", payload);

            return NoContent();
        }

        [HttpPut("messages/{id:int}/reactions")]
        public async Task<IActionResult> React(int id, [FromBody] ToggleReactionDto model)
        {
            var updated = await _directMessages.ReactAsync(UserId, id, model);

            await _hubContext.Clients.User(updated.UserId).SendAsync("DirectMessageReaction", updated.ForUser);
            await _hubContext.Clients.User(updated.RecipientId).SendAsync("DirectMessageReaction", updated.ForRecipient);

            return Ok(updated.ForUser);
        }

        [HttpGet("{userName}/pins")]
        public async Task<IActionResult> GetPins(string userName)
        {
            return Ok(await _directMessages.GetPinsAsync(UserId, userName));
        }

        [HttpPut("messages/{id:int}/pin")]
        public async Task<IActionResult> Pin(int id, [FromBody] PinMessageDto model)
        {
            var updated = await _directMessages.PinAsync(UserId, id, model);

            await _hubContext.Clients.User(updated.UserId).SendAsync("DirectMessagePinned", new { messageId = id, pinnedAt = updated.ForUser.PinnedAt });
            await _hubContext.Clients.User(updated.RecipientId).SendAsync("DirectMessagePinned", new { messageId = id, pinnedAt = updated.ForRecipient.PinnedAt });

            return Ok(updated.ForUser);
        }

        [HttpPost("{userName}/read")]
        public async Task<IActionResult> Read(string userName)
        {
            await _directMessages.MarkReadAsync(UserId, userName);
            return Ok();
        }

        [HttpDelete("{userName}")]
        public async Task<IActionResult> Close(string userName)
        {
            await _directMessages.CloseAsync(UserId, userName);
            return Ok();
        }
    }
}
