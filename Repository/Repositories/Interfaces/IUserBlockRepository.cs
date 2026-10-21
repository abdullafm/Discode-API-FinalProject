using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IUserBlockRepository : IBaseRepository<UserBlock>
    {
        Task<UserBlock?> GetBlockAsync(string blockerId, string blockedId);
        Task<bool> IsBlockedEitherWayAsync(string userIdA, string userIdB);
        Task<IEnumerable<UserBlock>> GetBlockedByUserAsync(string blockerId); 
    }
}
