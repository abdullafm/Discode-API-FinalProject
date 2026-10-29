using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Hubs
{
    public class DiscoverBroadcaster
    {
        private readonly IServerService _servers;
        private readonly IHubContext<PresenceHub> _hub;

        public DiscoverBroadcaster(IServerService servers, IHubContext<PresenceHub> hub)
        {
            _servers = servers;
            _hub = hub;
        }

        public async Task PublishServerAsync(int serverId)
        {
            if (!PresenceHub.HasDiscoverWatchers)
            {
                return;
            }

            var item = await _servers.GetDiscoverItemAsync(serverId);
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

            foreach (var item in await _servers.GetDiscoverForUserAsync(userId))
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
