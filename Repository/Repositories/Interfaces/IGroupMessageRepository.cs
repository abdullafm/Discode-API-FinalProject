using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupMessageRepository : IBaseRepository<GroupMessage>
    {
        Task<IEnumerable<GroupMessage>> GetLatestAsync(int groupId, int count, int? beforeId = null);
        Task<GroupMessage?> GetWithSenderAsync(int id);
        Task ClearRepliesAsync(int messageId);
        Task<Dictionary<int, DateTime>> GetLastActivityAsync(IReadOnlyCollection<int> groupIds);
        Task ToggleReactionAsync(int messageId, string userId, string emoji);
    }
}
