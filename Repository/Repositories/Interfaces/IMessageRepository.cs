using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IMessageRepository : IBaseRepository<Message>
    {
        Task<IEnumerable<Message>> GetLatestAsync(int conversationId, int count);
        Task<Message?> GetWithDetailsAsync(int id);
    }
}
