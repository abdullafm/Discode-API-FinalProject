using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupCallRepository : IBaseRepository<GroupCall>
    {
        Task<GroupCall?> GetFullAsync(int id);
        Task<GroupCall?> GetActiveForGroupAsync(int groupId);
        Task<IEnumerable<GroupCall>> GetActiveForGroupsAsync(IReadOnlyCollection<int> groupIds);
        Task<IEnumerable<GroupCall>> GetActiveForUserAsync(string userId);
        Task SetParticipantAsync(int callId, string userId, bool inCall);
        Task SetScreenAsync(int callId, string userId, bool on);
        Task EndAsync(int callId);
        Task EndAllOpenAsync();
    }
}
