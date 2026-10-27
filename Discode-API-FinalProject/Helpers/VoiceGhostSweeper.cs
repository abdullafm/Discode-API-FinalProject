using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Helpers
{
    public class VoiceGhostSweeper : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopes;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly Dictionary<string, DateTime> _offlineSince = new();

        public VoiceGhostSweeper(IServiceScopeFactory scopes, IHubContext<PresenceHub> hubContext)
        {
            _scopes = scopes;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (await WaitAsync(timer, stoppingToken))
            {
                try
                {
                    await SweepAsync();
                }
                catch (Exception)
                {
                }
            }
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
        {
            try
            {
                return await timer.WaitForNextTickAsync(token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task SweepAsync()
        {
            using var scope = _scopes.CreateScope();
            var states = scope.ServiceProvider.GetRequiredService<IServerVoiceStateRepository>();
            var tracker = scope.ServiceProvider.GetRequiredService<IPresenceTrackerRepository>();
            var voice = scope.ServiceProvider.GetRequiredService<IServerVoiceService>();

            var inVoice = await states.GetAllUserIdsAsync();
            var now = DateTime.UtcNow;
            foreach (var gone in _offlineSince.Keys.Where(id => !inVoice.Contains(id)).ToList())
            {
                _offlineSince.Remove(gone);
            }

            foreach (var userId in inVoice)
            {
                if (await tracker.IsOnlineAsync(userId))
                {
                    _offlineSince.Remove(userId);
                    continue;
                }

                if (!_offlineSince.TryGetValue(userId, out var since))
                {
                    _offlineSince[userId] = now;
                    continue;
                }

                if (now - since < Patience)
                {
                    continue;
                }

                _offlineSince.Remove(userId);
                foreach (var serverId in (await voice.LeaveAsync(userId)).ServerIds)
                {
                    await _hubContext.Clients.Users(await voice.GetMemberIdsAsync(serverId)).SendAsync("ServerVoiceChanged", new { serverId });
                }
            }
        }
    }
}
