using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IPollRepository : IBaseRepository<Poll>
    {
        Task<Poll?> GetByMessageIdAsync(int messageId);
        Task SetVotesAsync(Poll poll, string userId, IReadOnlyCollection<int> optionIds);
    }
}
