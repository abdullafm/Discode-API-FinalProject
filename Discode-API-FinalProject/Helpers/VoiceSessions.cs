using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Helpers
{
    public enum VoiceKind
    {
        DirectCall,
        GroupCall,
        ServerChannel
    }

    public class VoiceSessions
    {
        private readonly ICallService _calls;
        private readonly IGroupCallService _groupCalls;
        private readonly IServerVoiceService _serverVoice;
        private readonly IHubContext<PresenceHub> _hubContext;

        public VoiceSessions(ICallService calls, IGroupCallService groupCalls, IServerVoiceService serverVoice, IHubContext<PresenceHub> hubContext)
        {
            _calls = calls;
            _groupCalls = groupCalls;
            _serverVoice = serverVoice;
            _hubContext = hubContext;
        }

        public async Task LeaveOthersAsync(string userId, VoiceKind keep)
        {
            if (keep != VoiceKind.DirectCall)
            {
                foreach (var ended in await _calls.EndAllForAsync(userId))
                {
                    await PresenceHub.AnnounceCallEndAsync(_hubContext.Clients, ended);
                }
            }

            if (keep != VoiceKind.GroupCall)
            {
                foreach (var change in await _groupCalls.LeaveAllForAsync(userId))
                {
                    await PresenceHub.AnnounceGroupCallAsync(_hubContext.Clients, change);
                }
            }

            if (keep != VoiceKind.ServerChannel)
            {
                foreach (var serverId in (await _serverVoice.LeaveAsync(userId)).ServerIds.Distinct())
                {
                    await _hubContext.Clients.Users(await _serverVoice.GetMemberIdsAsync(serverId)).SendAsync("ServerVoiceChanged", new { serverId });
                }
            }
        }
    }
}
