using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IUserIgnoreRepository : IBaseRepository<UserIgnore>
    {
        Task<UserIgnore?> GetAsync(string ignorerId, string ignoredId);
        Task<IEnumerable<UserIgnore>> GetIgnoredByUserAsync(string ignorerId);
    }
}
