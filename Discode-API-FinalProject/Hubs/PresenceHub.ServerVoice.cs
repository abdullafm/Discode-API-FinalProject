using Microsoft.AspNetCore.SignalR;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        public async Task SendServerVoiceSignal(string toUserId, string payload)
        {
            if (string.IsNullOrEmpty(payload) || payload.Length > MaxCallSignalLength || toUserId == UserId)
            {
                return;
            }

            if (await _voiceService.AreTogetherAsync(UserId, toUserId))
            {
                await Clients.User(toUserId).SendAsync("ServerVoiceSignal", new { fromUserId = UserId, payload });
            }
        }
    }
}
