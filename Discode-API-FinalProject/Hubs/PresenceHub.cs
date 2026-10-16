using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Presence;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Hubs
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly IPresenceService _presenceService;
        private readonly IPresenceTrackerService _trackerService;

        public PresenceHub(IPresenceService presenceService, IPresenceTrackerService trackerService)
        {
            _presenceService = presenceService;
            _trackerService = trackerService;
        }

        private string UserId
        {
            get
            {
                return Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        public override async Task OnConnectedAsync()
        {
            var change = await _trackerService.ConnectAsync(UserId, Context.ConnectionId);
            await Clients.Caller.SendAsync("StatusChanged", change.PreferredStatus);

            if (change.ShouldBroadcast)
            {
                await NotifyFriendsAsync(change);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var change = await _trackerService.DisconnectAsync(UserId, Context.ConnectionId);
            if (change is not null)
            {
                await NotifyFriendsAsync(change);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SetStatus(string status)
        {
            try
            {
                var change = await _presenceService.SetStatusAsync(UserId, status);
                await Clients.User(UserId).SendAsync("StatusChanged", change.PreferredStatus);
                await NotifyFriendsAsync(change);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        private Task NotifyFriendsAsync(PresenceChangeDto change)
        {
            return Clients.Users(change.FriendIds).SendAsync("PresenceChanged", new
            {
                userId = change.UserId,
                status = change.Status
            });
        }
    }
}
