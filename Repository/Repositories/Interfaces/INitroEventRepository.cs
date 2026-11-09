using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public class NitroPaymentTotal
    {
        public string UserId { get; set; } = string.Empty;
        public long Cents { get; set; }
        public int Count { get; set; }
        public DateTime? LastAt { get; set; }
    }

    public interface INitroEventRepository : IBaseRepository<NitroEvent>
    {
        Task<bool> ExistsForStripeEventAsync(string stripeEventId);
        // what these people paid for Nitro (subscriptions, renewals and gifts they bought): how much, how many payments, the last one
        Task<List<NitroPaymentTotal>> GetPaymentTotalsAsync(IEnumerable<string> userIds);
        // all money taken for Nitro, and the part of it since the given time
        Task<(long Total, long Since)> GetRevenueAsync(DateTime since);
        Task<(List<NitroEvent> Items, int Total)> GetPageAsync(string? search, int page, int pageSize);
    }
}
