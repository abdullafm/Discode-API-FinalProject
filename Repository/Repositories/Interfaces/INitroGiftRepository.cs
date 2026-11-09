using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface INitroGiftRepository
    {
        Task AddAsync(NitroGift gift);
        Task UpdateAsync(NitroGift gift);
        Task<NitroGift?> GetByCodeAsync(string code);
        Task<NitroGift?> GetByIdAsync(int id);
        Task<NitroGift?> GetByStripeSessionAsync(string sessionId);
        Task<List<NitroGift>> GetBoughtByAsync(string buyerId);
        Task<(List<NitroGift> Items, int Total)> GetPageAsync(string? search, int page, int size);
    }
}
