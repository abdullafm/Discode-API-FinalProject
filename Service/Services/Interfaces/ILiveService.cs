using Service.Helpers.DTOs.Live;

namespace Service.Services.Interfaces
{
    public interface ILiveService
    {
        Task<LiveStreamDto> StartAsync(string userId, string connectionId, StartLiveDto model);
        Task<IEnumerable<int>> StopAsync(string userId);
        Task<IEnumerable<LiveStreamDto>> GetActiveAsync(string viewerId);
        Task<LiveJoinDto> JoinAsync(string viewerId, string connectionId, int streamId);
        Task SetThumbnailAsync(string userId, LiveThumbnailDto model);
        Task<int> LeaveAsync(string connectionId, int streamId);
        Task<bool> CanSignalAsync(string connectionId, int streamId, string targetConnectionId);
        Task<LiveMessageDto> CreateMessageAsync(string userId, string connectionId, int streamId, string text);
        Task<LiveDisconnectDto> DisconnectAsync(string connectionId);
    }
}
