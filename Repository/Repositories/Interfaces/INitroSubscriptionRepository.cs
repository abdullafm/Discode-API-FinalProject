using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface INitroSubscriptionRepository : IBaseRepository<NitroSubscription>
    {
        Task<NitroSubscription?> GetByUserAsync(string userId);
        Task<NitroSubscription?> GetByStripeSubscriptionAsync(string stripeSubscriptionId);
        Task<NitroSubscription?> GetByStripeCustomerAsync(string stripeCustomerId);
        // rows that still say Active/PastDue although their paid time is over
        Task<List<NitroSubscription>> GetOverdueAsync(DateTime now);
        Task<HashSet<string>> GetActiveUserIdsAsync(IEnumerable<string> userIds, DateTime now);
        Task<(List<NitroSubscription> Items, int Total)> GetPageAsync(string? search, bool activeOnly, DateTime now, int page, int pageSize);
    }
}
