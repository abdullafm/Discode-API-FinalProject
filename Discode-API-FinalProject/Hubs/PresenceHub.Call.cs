using Microsoft.AspNetCore.SignalR;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Calls;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        private const int MaxCallSignalLength = 16000;
        private static readonly TimeSpan CallReconnectGrace = TimeSpan.FromSeconds(8);

        public async Task<CallDto> StartCall(string userName, bool video)
        {
            try
            {
                var pair = await _callService.StartAsync(UserId, userName, video);
                await Clients.User(pair.CalleeId).SendAsync("IncomingCall", pair.ForCallee);
                return pair.ForCaller;
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

        public async Task AcceptCall(string callId)
        {
            try
            {
                var pair = _callService.Accept(UserId, callId);
                await Clients.User(pair.CallerId).SendAsync("CallAccepted", pair.ForCaller);
                await Clients.User(pair.CalleeId).SendAsync("CallAccepted", pair.ForCallee);
            }
            catch (NotFoundException ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task DeclineCall(string callId)
        {
            await EndCall(callId, false);
        }

        public async Task EndCall(string callId, bool timedOut)
        {
            var ended = _callService.End(UserId, callId, timedOut);
            if (ended is not null)
            {
                await Clients.Users(ended.CallerId, ended.CalleeId).SendAsync("CallEnded", ended);
            }
        }

        public async Task SendCallSignal(string callId, string payload)
        {
            if (string.IsNullOrEmpty(payload) || payload.Length > MaxCallSignalLength)
            {
                return;
            }

            var peerId = _callService.GetPeerId(UserId, callId);
            if (peerId is not null)
            {
                await Clients.User(peerId).SendAsync("CallSignal", new { callId, payload });
            }
        }

        public Task<CallDto?> GetMyCall()
        {
            return Task.FromResult(_callService.GetFor(UserId));
        }

        private static async Task EndCallsAfterGraceAsync(string userId, IServiceScopeFactory scopes, IHubContext<PresenceHub> hubContext)
        {
            await Task.Delay(CallReconnectGrace);

            using var scope = scopes.CreateScope();
            var tracker = scope.ServiceProvider.GetRequiredService<IPresenceTrackerRepository>();
            if (await tracker.IsOnlineAsync(userId))
            {
                return;
            }

            var calls = scope.ServiceProvider.GetRequiredService<ICallService>();
            foreach (var ended in calls.EndAllFor(userId))
            {
                await hubContext.Clients.Users(ended.CallerId, ended.CalleeId).SendAsync("CallEnded", ended);
            }
        }
    }
}
