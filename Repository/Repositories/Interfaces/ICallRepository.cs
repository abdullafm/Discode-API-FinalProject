using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICallRepository : IBaseRepository<Call>
    {
        Task<Call?> GetOpenByIdAsync(int id);
        Task<Call?> GetOpenBetweenAsync(string userId, string otherUserId);
        Task<IEnumerable<Call>> GetOpenForUserAsync(string userId);
        Task EndExpiredRingingAsync(DateTime startedBefore);
        Task EndAllOpenAsync();
    }
}
