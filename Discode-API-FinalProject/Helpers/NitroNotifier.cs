using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Nitro;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Helpers
{
    // Live news: the person's own pages learn that Nitro changed, the members of a boosted server that its boosts changed
    public static class NitroNotifier
    {
        public static async Task TellAsync(IHubContext<PresenceHub> hub, IBoostService boosts, NitroChangeDto change)
        {
            if (change.UserIds.Count > 0)
            {
                await hub.Clients.Users(change.UserIds).SendAsync("NitroChanged", new { });
            }

            foreach (var serverId in change.ServerIds.Distinct())
            {
                await hub.Clients.Users(await boosts.GetMemberIdsAsync(serverId)).SendAsync("ServerBoostChanged", new { serverId });
            }
        }
    }
}
