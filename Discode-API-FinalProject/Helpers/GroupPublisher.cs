using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Groups;

namespace Discode_API_FinalProject.Helpers
{
    public class GroupPublisher
    {
        private readonly IHubContext<PresenceHub> _hubContext;

        public GroupPublisher(IHubContext<PresenceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishAsync(IEnumerable<GroupChangeDto> changes)
        {
            foreach (var change in changes)
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
}
