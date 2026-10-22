using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupMessageRepository : IBaseRepository<GroupMessage>
    {
        Task<IEnumerable<GroupMessage>> GetLatestAsync(int groupId, int count);
        Task<GroupMessage?> GetWithSenderAsync(int id);
        Task ClearRepliesAsync(int messageId);
        Task ToggleReactionAsync(int messageId, string userId, string emoji);
    }
}
