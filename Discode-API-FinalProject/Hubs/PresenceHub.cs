using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Presence;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Hubs
{
    [Authorize]
    public partial class PresenceHub : Hub
    {
        private readonly IPresenceService _presenceService;
        private readonly IPresenceTrackerService _trackerService;
        private readonly ILiveStreamService _liveStreamService;

        public PresenceHub(IPresenceService presenceService, IPresenceTrackerService trackerService, ILiveStreamService liveStreamService)
        {
            _presenceService = presenceService;
            _trackerService = trackerService;
            _liveStreamService = liveStreamService;
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

            await ReleaseLiveAsync();

            await base.OnDisconnectedAsync(exception);
        }

        public static string ProfileGroup(string userId)
        {
            return "profile-" + userId;
        }

        public Task WatchProfile(string userId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, ProfileGroup(userId));
        }

        public Task UnwatchProfile(string userId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, ProfileGroup(userId));
        }

        public async Task SetAway(bool away)
        {
            var change = await _trackerService.SetAwayAsync(UserId, Context.ConnectionId, away);
            await NotifyFriendsAsync(change);
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
