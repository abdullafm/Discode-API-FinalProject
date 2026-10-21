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
        string? GetPeerId(string userId, string callId);
    }
}
