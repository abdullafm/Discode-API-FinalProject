using Service.Helpers.DTOs.Live;

namespace Service.Services.Interfaces
{
    public interface ILiveStreamService
    {
        Task<LiveStreamDto> StartAsync(string userId, string connectionId, StartLiveDto model);
        Task<IEnumerable<int>> StopAsync(string userId);
        Task<IEnumerable<LiveStreamDto>> GetActiveAsync(string viewerId, bool staff = false);
        Task<List<Service.Helpers.DTOs.Admin.AdminLiveStreamDto>> GetActiveForAdminAsync();
        Task<LiveJoinDto> JoinAsync(string viewerId, string connectionId, int streamId, bool staff = false);
        Task SetThumbnailAsync(string userId, LiveThumbnailDto model);
        Task<IEnumerable<LiveViewerDto>> GetViewersAsync(string connectionId, int streamId);
        Task<LiveGuestRequestDto> RequestGuestAsync(string connectionId, int streamId);
        Task<LiveGuestInviteDto> InviteGuestAsync(string hostConnectionId, int streamId, string userName);
        Task<LiveGuestInviteAnswerDto> AnswerGuestInviteAsync(string connectionId, int streamId, bool accept);
        Task<LiveGuestDto?> RespondGuestAsync(string hostConnectionId, int streamId, string targetConnectionId, bool accept);
        Task<string?> EndGuestAsync(string connectionId, int streamId);
        Task<int> LeaveAsync(string connectionId, int streamId);
        Task<List<LiveEjectedDto>> EjectBlockedAsync(string blockerId, string blockedId);
        Task<LiveInviteResultDto> InviteAsync(string userId, int streamId, IEnumerable<string> userNames);
        Task<bool> CanSignalAsync(string connectionId, int streamId, string targetConnectionId);
        Task<LiveMessageDto> CreateMessageAsync(string userId, string connectionId, int streamId, string text, string? replyUserName = null, string? replyText = null);
        Task<LiveDisconnectDto> DisconnectAsync(string connectionId);
    }
}
