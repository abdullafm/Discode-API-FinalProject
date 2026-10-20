using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IMessageReactionRepository : IBaseRepository<MessageReaction>
    {
        Task<MessageReaction?> GetByUserAsync(int messageId, string userId, string emoji);
    }
}
