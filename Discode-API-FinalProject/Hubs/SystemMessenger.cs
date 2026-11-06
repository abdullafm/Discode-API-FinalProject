using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Hubs
{
    // Sends a direct message from the built-in "Discode" account and pushes it live
    public class SystemMessenger
    {
        private readonly IDirectMessageService _directMessages;
        private readonly IHubContext<PresenceHub> _hub;

        public SystemMessenger(IDirectMessageService directMessages, IHubContext<PresenceHub> hub)
        {
            _directMessages = directMessages;
            _hub = hub;
        }

        public async Task SendAsync(string recipientId, string text)
        {
            var sent = await _directMessages.SendSystemAsync(recipientId, text);
            await _hub.Clients.User(recipientId).SendAsync("DirectMessage", new
            {
                message = sent.MessageForRecipient,
                conversation = sent.ConversationForRecipient
            });
        }
    }
}
