using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Helpers
{
    public class StatusClearWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly ILogger<StatusClearWorker> _logger;

        public StatusClearWorker(IServiceScopeFactory scopes, IHubContext<PresenceHub> hubContext, ILogger<StatusClearWorker> logger)
        {
            _scopes = scopes;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (await WaitAsync(timer, stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
                    var presence = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                    foreach (var userId in await accounts.ClearExpiredStatusesAsync())
                    {
                        await _hubContext.Clients.Group(PresenceHub.ProfileGroup(userId)).SendAsync("ProfileChanged", new { userId });
                        var audience = (await presence.GetFriendIdsAsync(userId)).Union(await presence.GetSharedSpaceIdsAsync(userId)).Append(userId).Distinct().ToList();
                        await _hubContext.Clients.Users(audience).SendAsync("ProfileChanged", new { userId });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Status clearing failed");
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
    }
}
