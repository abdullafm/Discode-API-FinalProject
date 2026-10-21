using Service.Helpers.DTOs.Calls;

namespace Service.Services.Interfaces
{
    public interface ICallService
    {
        Task<CallPairDto> StartAsync(string callerId, string userName, bool video);
        CallPairDto Accept(string userId, string callId);
        Task<CallEndedDto?> EndAsync(string userId, string callId, bool timedOut);
        Task<IEnumerable<CallEndedDto>> EndAllForAsync(string userId);
        CallDto? GetFor(string userId);
        CallDto? SetScreen(string userId, string callId, bool on, out string peerId, out bool peerIn);
        string? GetPeerId(string userId, string callId);
        int? GetOngoingMessageId(string userId, string otherUserId);
    }
}
