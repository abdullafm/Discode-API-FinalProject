using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Live;
using Service.Helpers.Exceptions;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        public static string LiveGroup(int streamId)
        {
            return "live-" + streamId;
        }

        public async Task<LiveStreamDto> StartLive(StartLiveDto model)
        {
            try
            {
                await EnsureNotTimedOutAsync();
                await EndOwnStreamsAsync();
                var stream = await _liveStreamService.StartAsync(UserId, Context.ConnectionId, model);
                await Groups.AddToGroupAsync(Context.ConnectionId, LiveGroup(stream.Id));
                await Clients.All.SendAsync("LiveChanged");
                return stream;
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task UpdateLiveThumbnail(LiveThumbnailDto model)
        {
            try
            {
                await _liveStreamService.SetThumbnailAsync(UserId, model);
                await Clients.All.SendAsync("LiveChanged");
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task StopLive()
        {
            await EndOwnStreamsAsync();
            await Clients.All.SendAsync("LiveChanged");
        }

        public async Task<LiveJoinDto> JoinLive(int streamId)
        {
            try
            {
                var staff = Context.User?.IsInRole(Domain.Constants.Roles.SuperAdmin) == true || Context.User?.IsInRole(Domain.Constants.Roles.Admin) == true;
                var join = await _liveStreamService.JoinAsync(UserId, Context.ConnectionId, streamId, staff);
                await Groups.AddToGroupAsync(Context.ConnectionId, LiveGroup(streamId));
                await Clients.Client(join.HostConnectionId).SendAsync("ViewerJoined", streamId, Context.ConnectionId);
                await Clients.All.SendAsync("LiveViewers", streamId, join.Stream.ViewerCount);
                await Clients.Group(LiveGroup(streamId)).SendAsync("LiveJoined", join.Viewer);
                return join;
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task<IEnumerable<LiveViewerDto>> GetLiveViewers(int streamId)
        {
            try
            {
                return await _liveStreamService.GetViewersAsync(Context.ConnectionId, streamId);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task RequestLiveGuest(int streamId)
        {
            try
            {
                var request = await _liveStreamService.RequestGuestAsync(Context.ConnectionId, streamId);
                await Clients.Client(request.HostConnectionId).SendAsync("GuestRequested", request.Guest);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task InviteLiveGuest(int streamId, string userName)
        {
            try
            {
                var invite = await _liveStreamService.InviteGuestAsync(Context.ConnectionId, streamId, userName);
                await Clients.Client(invite.TargetConnectionId).SendAsync("GuestInvited", invite);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task AnswerLiveGuestInvite(int streamId, bool accept)
        {
            try
            {
                var answer = await _liveStreamService.AnswerGuestInviteAsync(Context.ConnectionId, streamId, accept);
                await Clients.Client(answer.HostConnectionId).SendAsync("GuestInviteAnswered", streamId, answer.Guest is not null, answer.DisplayName);
                if (answer.Guest is not null)
                {
                    await Clients.Caller.SendAsync("GuestResponse", streamId, true);
                    await Clients.Group(LiveGroup(streamId)).SendAsync("GuestStarted", answer.Guest);
                }
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task SetLiveModerator(int streamId, string userName, bool on)
        {
            var result = await _liveModerationService.SetModeratorAsync(Context.ConnectionId, UserId, streamId, userName, on);
            foreach (var connection in result.TargetConnectionIds)
            {
                await Clients.Client(connection).SendAsync("LiveModeratorChanged", streamId, result.IsModerator);
            }
        }

        public async Task MuteLiveViewer(int streamId, string userName, int minutes)
        {
            var result = await _liveModerationService.MuteAsync(Context.ConnectionId, UserId, streamId, userName, minutes);
            foreach (var connection in result.TargetConnectionIds)
            {
                await Clients.Client(connection).SendAsync("LiveMuteChanged", streamId, true, result.MutedUntil);
                if (result.ModeratorRemoved)
                {
                    await Clients.Client(connection).SendAsync("LiveModeratorChanged", streamId, false);
                }
            }

            if (result.EndedGuestConnectionId is not null)
            {
                await Clients.Group(LiveGroup(streamId)).SendAsync("GuestEnded", streamId, result.EndedGuestConnectionId);
            }
        }

        public async Task UnmuteLiveViewer(int streamId, string userName)
        {
            var result = await _liveModerationService.UnmuteAsync(Context.ConnectionId, UserId, streamId, userName);
            foreach (var connection in result.TargetConnectionIds)
            {
                await Clients.Client(connection).SendAsync("LiveMuteChanged", streamId, false, (DateTime?)null);
            }
        }

        public Task<List<LiveMuteDto>> GetLiveMutes(int streamId)
        {
            return _liveModerationService.GetMutesAsync(Context.ConnectionId, UserId, streamId);
        }

        public async Task RespondLiveGuest(int streamId, string targetConnectionId, bool accept)
        {
            try
            {
                var guest = await _liveStreamService.RespondGuestAsync(Context.ConnectionId, streamId, targetConnectionId, accept);
                await Clients.Client(targetConnectionId).SendAsync("GuestResponse", streamId, guest is not null);
                if (guest is not null)
                {
                    await Clients.Group(LiveGroup(streamId)).SendAsync("GuestStarted", guest);
                }
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task EndLiveGuest(int streamId)
        {
            var ended = await _liveStreamService.EndGuestAsync(Context.ConnectionId, streamId);
            if (ended is not null)
            {
                await Clients.Group(LiveGroup(streamId)).SendAsync("GuestEnded", streamId, ended);
            }
        }

        public async Task LeaveLive(int streamId)
        {
            var count = await _liveStreamService.LeaveAsync(Context.ConnectionId, streamId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, LiveGroup(streamId));
            await Clients.Group(LiveGroup(streamId)).SendAsync("ViewerLeft", streamId, Context.ConnectionId);
            await Clients.All.SendAsync("LiveViewers", streamId, count);
        }

        public async Task SendSignal(int streamId, string targetConnectionId, string payload)
        {
            if (!await _liveStreamService.CanSignalAsync(Context.ConnectionId, streamId, targetConnectionId))
            {
                throw new HubException("This stream is not live.");
            }

            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", streamId, Context.ConnectionId, payload);
        }

        public async Task SendLiveReply(int streamId, string text, string replyUserName, string replyText)
        {
            try
            {
                await EnsureNotTimedOutAsync();
                var message = await _liveStreamService.CreateMessageAsync(UserId, Context.ConnectionId, streamId, text, replyUserName, replyText);
                await Clients.Group(LiveGroup(streamId)).SendAsync("LiveMessage", message);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task SendLiveMessage(int streamId, string text)
        {
            try
            {
                await EnsureNotTimedOutAsync();
                var message = await _liveStreamService.CreateMessageAsync(UserId, Context.ConnectionId, streamId, text);
                await Clients.Group(LiveGroup(streamId)).SendAsync("LiveMessage", message);
            }
            catch (BadRequestException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task SendLiveHeart(int streamId)
        {
            if (!await _liveStreamService.CanSignalAsync(Context.ConnectionId, streamId, Context.ConnectionId))
            {
                return;
            }

            await Clients.Group(LiveGroup(streamId)).SendAsync("LiveHeart", streamId);
        }

        private async Task EndOwnStreamsAsync()
        {
            var ended = await _liveStreamService.StopAsync(UserId);
            foreach (var id in ended)
            {
                await Clients.Group(LiveGroup(id)).SendAsync("LiveEnded", id);
            }
        }

        private async Task ReleaseLiveAsync()
        {
            var result = await _liveStreamService.DisconnectAsync(Context.ConnectionId);

            foreach (var id in result.EndedStreamIds)
            {
                await Clients.Group(LiveGroup(id)).SendAsync("LiveEnded", id);
            }

            foreach (var pair in result.ViewerCounts)
            {
                await Clients.Group(LiveGroup(pair.Key)).SendAsync("ViewerLeft", pair.Key, Context.ConnectionId);
                await Clients.All.SendAsync("LiveViewers", pair.Key, pair.Value);
            }

            if (result.EndedStreamIds.Any())
            {
                await Clients.All.SendAsync("LiveChanged");
            }
        }
    }
}
