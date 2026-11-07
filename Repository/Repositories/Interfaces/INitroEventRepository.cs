using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface INitroEventRepository : IBaseRepository<NitroEvent>
    {
        Task<bool> ExistsForStripeEventAsync(string stripeEventId);
        Task<(List<NitroEvent> Items, int Total)> GetPageAsync(string? search, int page, int pageSize);
    }
}
