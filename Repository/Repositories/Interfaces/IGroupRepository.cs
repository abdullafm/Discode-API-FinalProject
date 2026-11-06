using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupRepository : IBaseRepository<Group>
    {
        Task<Group?> GetWithMembersAsync(int id);
        Task<IEnumerable<Group>> GetForUserAsync(string userId);
        Task<Group?> FindWithSameMembersAsync(IReadOnlyCollection<string> userIds);
        Task<Group?> GetTrackedWithMembersAsync(int id);
        Task AddMembersAsync(int groupId, IEnumerable<string> userIds);
        Task RemoveMemberAsync(int groupId, string userId);
        Task<Dictionary<int, int>> GetUnreadCountsAsync(string userId);
        Task MarkReadAsync(int groupId, string userId);
    }
}
