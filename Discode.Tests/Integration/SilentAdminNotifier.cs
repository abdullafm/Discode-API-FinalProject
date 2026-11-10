using Repository.Notifications;

namespace Discode.Tests.Integration
{
    public class SilentAdminNotifier : IAdminChangeNotifier
    {
        public Task NotifyAsync(IReadOnlyCollection<string> topics, IReadOnlyCollection<int> serverIds)
        {
            return Task.CompletedTask;
        }
    }
}
