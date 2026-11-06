using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Hubs
{
    public class DiscoverBroadcaster
    {
        private readonly IDiscoverService _discover;
        private readonly IHubContext<PresenceHub> _hub;

        public DiscoverBroadcaster(IDiscoverService discover, IHubContext<PresenceHub> hub)
        {
            _discover = discover;
            _hub = hub;
        }

        public async Task PublishServerAsync(int serverId)
        {
            if (!PresenceHub.HasDiscoverWatchers)
            {
                return;
            }

            var item = await _discover.GetItemAsync(serverId);
            if (item is null)
            {
                await _hub.Clients.Group(PresenceHub.DiscoverGroup).SendAsync("DiscoverServerRemoved", new { serverId });
                return;
            }

            await _hub.Clients.Group(PresenceHub.DiscoverGroup).SendAsync("DiscoverServer", item);
        }

        public async Task PublishUserServersAsync(string userId)
        {
            if (!PresenceHub.HasDiscoverWatchers)
            {
                return;
            }

            foreach (var item in await _discover.GetForUserAsync(userId))
            {
                await _hub.Clients.Group(PresenceHub.DiscoverGroup).SendAsync("DiscoverServer", item);
            }
        }

        public Task PublishRemovedAsync(int serverId)
        {
            if (!PresenceHub.HasDiscoverWatchers)
            {
                return Task.CompletedTask;
            }

            return _hub.Clients.Group(PresenceHub.DiscoverGroup).SendAsync("DiscoverServerRemoved", new { serverId });
        }
    }
}
