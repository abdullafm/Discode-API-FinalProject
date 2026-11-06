using Microsoft.AspNetCore.SignalR;
using Repository.Notifications;

namespace Discode_API_FinalProject.Hubs
{
    public class AdminHubNotifier : IAdminChangeNotifier
    {
        private readonly IHubContext<PresenceHub> _hub;

        public AdminHubNotifier(IHubContext<PresenceHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyAsync(IReadOnlyCollection<string> topics, IReadOnlyCollection<int> serverIds)
        {
            return _hub.Clients.Group(PresenceHub.AdminGroup).SendAsync("AdminChanged", new { topics, serverIds });
        }
    }
}
