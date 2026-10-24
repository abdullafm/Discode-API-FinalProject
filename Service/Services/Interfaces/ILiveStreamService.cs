using Service.Helpers.DTOs.Live;

namespace Service.Services.Interfaces
{
    public interface ILiveStreamService
    {
        Task<LiveStreamDto> StartAsync(string userId, string connectionId, StartLiveDto model);
        Task<IEnumerable<int>> StopAsync(string userId);
        Task<IEnumerable<LiveStreamDto>> GetActiveAsync(string viewerId);
        Task<LiveJoinDto> JoinAsync(string viewerId, string connectionId, int streamId);
        Task SetThumbnailAsync(string userId, LiveThumbnailDto model);
        Task<IReadOnlyList<LiveViewerDto>> GetViewersAsync(string connectionId, int streamId);
        Task<LiveGuestRequestDto> RequestGuestAsync(string connectionId, int streamId);
        Task<LiveGuestDto?> RespondGuestAsync(string hostConnectionId, int streamId, string targetConnectionId, bool accept);
        Task<string?> EndGuestAsync(string connectionId, int streamId);
        Task<int> LeaveAsync(string connectionId, int streamId);
        Task<LiveInviteResultDto> InviteAsync(string userId, int streamId, IEnumerable<string> userNames);
        Task<bool> CanSignalAsync(string connectionId, int streamId, string targetConnectionId);
        Task<LiveMessageDto> CreateMessageAsync(string userId, string connectionId, int streamId, string text, string? replyUserName = null, string? replyText = null);
        Task<LiveDisconnectDto> DisconnectAsync(string connectionId);
    }
}
