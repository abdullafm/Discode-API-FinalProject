using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Live;
using Service.Helpers.Exceptions;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        private static string LiveGroup(int streamId)
        {
            return "live-" + streamId;
        }

        public async Task<LiveStreamDto> StartLive(StartLiveDto model)
        {
            try
            {
                await EndOwnStreamsAsync();
                var stream = await _liveService.StartAsync(UserId, Context.ConnectionId, model);
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

        public async Task StopLive()
        {
            await EndOwnStreamsAsync();
            await Clients.All.SendAsync("LiveChanged");
        }

        public async Task<LiveJoinDto> JoinLive(int streamId)
        {
            try
            {
                var join = await _liveService.JoinAsync(UserId, Context.ConnectionId, streamId);
                await Groups.AddToGroupAsync(Context.ConnectionId, LiveGroup(streamId));
                await Clients.Client(join.HostConnectionId).SendAsync("ViewerJoined", streamId, Context.ConnectionId);
                await Clients.All.SendAsync("LiveViewers", streamId, join.Stream.ViewerCount);
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

        public async Task LeaveLive(int streamId)
        {
            var count = await _liveService.LeaveAsync(Context.ConnectionId, streamId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, LiveGroup(streamId));
            await Clients.Group(LiveGroup(streamId)).SendAsync("ViewerLeft", streamId, Context.ConnectionId);
            await Clients.All.SendAsync("LiveViewers", streamId, count);
        }

        public async Task SendSignal(int streamId, string targetConnectionId, string payload)
        {
            if (!await _liveService.CanSignalAsync(Context.ConnectionId, streamId, targetConnectionId))
            {
                throw new HubException("This stream is not live.");
            }

            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", streamId, Context.ConnectionId, payload);
        }

        public async Task SendLiveMessage(int streamId, string text)
        {
            try
            {
                var message = await _liveService.CreateMessageAsync(UserId, Context.ConnectionId, streamId, text);
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
            if (!await _liveService.CanSignalAsync(Context.ConnectionId, streamId, Context.ConnectionId))
            {
                return;
            }

            await Clients.Group(LiveGroup(streamId)).SendAsync("LiveHeart", streamId);
        }

        private async Task EndOwnStreamsAsync()
        {
            var ended = await _liveService.StopAsync(UserId);
            foreach (var id in ended)
            {
                await Clients.Group(LiveGroup(id)).SendAsync("LiveEnded", id);
            }
        }

        private async Task ReleaseLiveAsync()
        {
            var result = await _liveService.DisconnectAsync(Context.ConnectionId);

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
