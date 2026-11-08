using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IBoostCooldownRepository : IBaseRepository<BoostCooldown>
    {
        Task<List<BoostCooldown>> GetActiveAsync(string userId, DateTime now);
        // forgets the ones that are over, and all of them when the person lost Nitro
        Task DeleteForUserAsync(string userId, DateTime? onlyBefore = null);
    }
}
