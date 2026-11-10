using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IBoostCooldownRepository : IBaseRepository<BoostCooldown>
    {
        Task<List<BoostCooldown>> GetActiveAsync(string userId, DateTime now);
        Task DeleteForUserAsync(string userId, DateTime? onlyBefore = null);
    }
}
