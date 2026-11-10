using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ILiveModerationRepository
    {
        Task<bool> IsModeratorAsync(string hostId, string userId);
        Task<HashSet<string>> GetModeratorIdsAsync(string hostId);
        Task AddModeratorAsync(string hostId, string userId);
        Task RemoveModeratorAsync(string hostId, string userId);
        Task<LiveChatMute?> GetActiveMuteAsync(string hostId, string userId, DateTime now);
        Task<List<LiveChatMute>> GetActiveMutesAsync(string hostId, DateTime now);
        Task SetMuteAsync(string hostId, string userId, DateTime? expiresAt, string mutedById);
        Task<bool> RemoveMuteAsync(string hostId, string userId);
    }
}
