using Service.Helpers.DTOs.Live;

namespace Service.Services.Interfaces
{
    public interface ILiveModerationService
    {
        Task<LiveModerationResultDto> SetModeratorAsync(string connectionId, string userId, int streamId, string userName, bool on);
        Task<LiveModerationResultDto> MuteAsync(string connectionId, string userId, int streamId, string userName, int minutes);
        Task<LiveModerationResultDto> UnmuteAsync(string connectionId, string userId, int streamId, string userName);
        Task<List<LiveMuteDto>> GetMutesAsync(string connectionId, string userId, int streamId);
    }
}
