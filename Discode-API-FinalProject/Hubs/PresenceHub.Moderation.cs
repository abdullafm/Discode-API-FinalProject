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

        private async Task DeliverWarningsAsync()
        {
            using var scope = _scopes.CreateScope();
            var warnings = await scope.ServiceProvider.GetRequiredService<IUserModerationService>().GetUnseenWarningsAsync(UserId);
            if (warnings.Count > 0)
            {
                await Clients.Caller.SendAsync("AccountWarning", warnings.Select(x => new { id = x.Id, reason = x.Reason, count = warnings.Count }));
            }
        }

        public async Task AcknowledgeWarning(int warningId)
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IUserModerationService>().AcknowledgeWarningAsync(UserId, warningId);
        }
    }
}
