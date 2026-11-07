using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Helpers
{
    // Ends the Nitro of people whose paid time is over, even if Stripe's news never reached us
    public class NitroExpiryWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopes;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly ILogger<NitroExpiryWorker> _logger;

        public NitroExpiryWorker(IServiceScopeFactory scopes, IHubContext<PresenceHub> hubContext, ILogger<NitroExpiryWorker> logger)
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
                    var nitro = scope.ServiceProvider.GetRequiredService<INitroService>();
                    var boosts = scope.ServiceProvider.GetRequiredService<IBoostService>();
                    await NitroNotifier.TellAsync(_hubContext, boosts, await nitro.ExpireOverdueAsync());
                }
                catch (Exception ex)
                {
                    // one bad round (database restarting, Stripe unreachable) must not stop the loop
                    _logger.LogError(ex, "Nitro expiry sweep failed");
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
