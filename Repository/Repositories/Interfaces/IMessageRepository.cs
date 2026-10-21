using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IMessageRepository : IBaseRepository<Message>
    {
        Task<IEnumerable<Message>> GetLatestAsync(int conversationId, int count);
        Task<Message?> GetWithDetailsAsync(int id);
<<<<<<< Updated upstream
=======
        Task<IEnumerable<Message>> GetPinnedAsync(int conversationId);
        Task<int> CountPinnedAsync(int conversationId);
        Task ClearRepliesToAsync(int messageId);
>>>>>>> Stashed changes
    }
}
