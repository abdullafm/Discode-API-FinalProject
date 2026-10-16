using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public interface IPresenceRepository
    {
        Task<AppUser?> GetUserAsync(string userId);
        Task<bool> SetPreferredStatusAsync(string userId, PresenceStatus status);
        Task<IEnumerable<string>> GetFriendIdsAsync(string userId);
    }
}
