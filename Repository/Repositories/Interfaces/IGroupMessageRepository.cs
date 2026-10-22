using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IGroupMessageRepository : IBaseRepository<GroupMessage>
    {
        Task<IEnumerable<GroupMessage>> GetLatestAsync(int groupId, int count);
        Task<GroupMessage?> GetWithSenderAsync(int id);
        Task ToggleReactionAsync(int messageId, string userId, string emoji);
    }
}
