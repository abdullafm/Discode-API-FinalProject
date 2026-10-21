using Service.Helpers.DTOs.Calls;

namespace Service.Services.Interfaces
{
    public interface ICallService
    {
        Task<CallPairDto> StartAsync(string callerId, string userName, bool video);
        CallPairDto Accept(string userId, string callId);
        CallEndedDto? End(string userId, string callId, bool timedOut);
        IEnumerable<CallEndedDto> EndAllFor(string userId);
        CallDto? GetFor(string userId);
    }
}
