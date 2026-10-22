using Service.Helpers.DTOs.Calls;

namespace Service.Services.Interfaces
{
    public interface ICallService
    {
        Task<CallPairDto> StartAsync(string callerId, string userName, bool video);
        Task<CallPairDto> AcceptAsync(string userId, string callId);
        Task<CallEndedDto?> EndAsync(string userId, string callId, bool timedOut);
        Task<IEnumerable<CallEndedDto>> EndAllForAsync(string userId);
        Task<IEnumerable<CallEndedDto>> EndIfNotAllowedAsync(string userId, string otherUserId);
        Task<CallDto?> GetForAsync(string userId);
        Task<CallScreenDto?> SetScreenAsync(string userId, string callId, bool on);
        Task<string?> GetPeerIdAsync(string userId, string callId);
        Task<int?> GetOngoingMessageIdAsync(string userId, string otherUserId);
    }
}
