using Service.Helpers.DTOs.Presence;

namespace Service.Services.Interfaces
{
    public interface IPresenceTrackerService
    {
        Task<PresenceChangeDto> ConnectAsync(string userId, string connectionId);
        Task<PresenceChangeDto?> DisconnectAsync(string userId, string connectionId);
        Task<PresenceChangeDto> SetAwayAsync(string userId, string connectionId, bool away);
    }
}
