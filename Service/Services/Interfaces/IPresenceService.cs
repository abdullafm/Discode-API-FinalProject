using Domain.Entities;
using Service.Helpers.DTOs.Presence;

namespace Service.Services.Interfaces
{
    public interface IPresenceService
    {
        Task<PresenceChangeDto> GetChangeAsync(string userId, bool shouldBroadcast);
        Task<PresenceChangeDto> SetStatusAsync(string userId, string status);
        Task<string> ResolveAsync(AppUser user);
        Task<IEnumerable<string>> GetFriendIdsAsync(string userId);
    }
}
