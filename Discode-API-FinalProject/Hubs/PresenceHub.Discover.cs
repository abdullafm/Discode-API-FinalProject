using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        public const string DiscoverGroup = "discover";

        private static readonly ConcurrentDictionary<string, byte> DiscoverWatchers = new();

        public static bool HasDiscoverWatchers => !DiscoverWatchers.IsEmpty;

        public async Task WatchDiscover()
        {
            DiscoverWatchers[Context.ConnectionId] = 0;
            await Groups.AddToGroupAsync(Context.ConnectionId, DiscoverGroup);
        }

        public async Task UnwatchDiscover()
        {
            DiscoverWatchers.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DiscoverGroup);
        }

        private void ForgetDiscoverWatcher()
        {
            DiscoverWatchers.TryRemove(Context.ConnectionId, out _);
        }

        private async Task PublishDiscoverForUserAsync(string userId)
        {
            if (!HasDiscoverWatchers)
            {
                return;
            }

            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DiscoverBroadcaster>().PublishUserServersAsync(userId);
            }
            catch (Exception)
            {
            }
        }
    }
}
