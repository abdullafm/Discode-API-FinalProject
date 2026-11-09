using Microsoft.AspNetCore.SignalR;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        private async Task<bool> IsBlockedAccountAsync()
        {
            using var scope = _scopes.CreateScope();
            var status = await scope.ServiceProvider.GetRequiredService<IUserModerationRepository>().GetStatusAsync(UserId);
            return status is not null && (status.IsDeleted || ModerationMessages.IsBanActive(status.IsBanned, status.BannedUntil, DateTime.UtcNow));
        }

        private async Task EnsureNotTimedOutAsync()
        {
            using var scope = _scopes.CreateScope();
            var status = await scope.ServiceProvider.GetRequiredService<IUserModerationRepository>().GetStatusAsync(UserId);
            if (status?.TimeoutUntil is DateTime until && until > DateTime.UtcNow)
            {
                throw new HubException(ModerationMessages.TimedOut(until, status.TimeoutReason));
            }
        }

        private async Task DeliverWarningsAsync()
        {
            using var scope = _scopes.CreateScope();
            var warnings = await scope.ServiceProvider.GetRequiredService<IUserModerationService>().GetUnseenWarningsAsync(UserId);
            if (warnings.Count > 0)
            {
                await Clients.Caller.SendAsync("AccountWarning", warnings.Select(x => new { id = x.Id, reason = x.Reason, count = warnings.Count }));
            }
        }

        private async Task DeliverTimeoutAsync()
        {
            using var scope = _scopes.CreateScope();
            var status = await scope.ServiceProvider.GetRequiredService<IUserModerationRepository>().GetStatusAsync(UserId);
            if (status?.TimeoutUntil is DateTime until && until > DateTime.UtcNow)
            {
                await Clients.Caller.SendAsync("AccountTimeout", new { seconds = (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds), until, reason = status.TimeoutReason });
            }
        }

        public async Task CheckTimeout()
        {
            using var scope = _scopes.CreateScope();
            var status = await scope.ServiceProvider.GetRequiredService<IUserModerationRepository>().GetStatusAsync(UserId);
            if (status?.TimeoutUntil is DateTime until && until > DateTime.UtcNow)
            {
                await Clients.Caller.SendAsync("AccountTimeout", new { seconds = (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds), until, reason = status.TimeoutReason });
            }
            else
            {
                await Clients.Caller.SendAsync("AccountTimeout", new { seconds = 0, until = (DateTime?)null, reason = string.Empty });
            }
        }

        public async Task AcknowledgeWarning(int warningId)
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IUserModerationService>().AcknowledgeWarningAsync(UserId, warningId);
        }
    }
}
