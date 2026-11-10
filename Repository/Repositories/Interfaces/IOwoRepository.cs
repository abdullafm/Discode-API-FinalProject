using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IOwoRepository
    {
        Task<OwoProfile> GetOrCreateProfileAsync(string userId, long startingCash);
        Task<List<OwoAnimal>> GetAnimalsAsync(string userId);
        Task AddAnimalsAsync(string userId, IEnumerable<string> keys);
        Task<List<OwoProfile>> TopAsync(IEnumerable<string> userIds, int take);
        Task SaveAsync();
    }
}
