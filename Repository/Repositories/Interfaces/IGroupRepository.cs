using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupRepository : IBaseRepository<Group>
    {
        Task<Group?> GetWithMembersAsync(int id);
        Task<IEnumerable<Group>> GetForUserAsync(string userId);
        Task<Group?> FindWithSameMembersAsync(IReadOnlyCollection<string> userIds);
    }
}
