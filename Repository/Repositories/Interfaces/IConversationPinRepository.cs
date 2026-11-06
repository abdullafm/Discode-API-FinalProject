using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IConversationPinRepository : IBaseRepository<ConversationPin>
    {
        Task<List<string>> GetKeysAsync(string userId);
        Task<ConversationPin?> GetAsync(string userId, string key);
    }
}
