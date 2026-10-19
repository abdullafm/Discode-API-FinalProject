using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IConversationRepository : IBaseRepository<Conversation>
    {
        Task<Conversation?> GetBetweenAsync(string userIdA, string userIdB);
        Task<IEnumerable<Conversation>> GetForUserAsync(string userId);
    }
}
