using Service.Helpers.DTOs.Groups;

namespace Service.Services.Interfaces
{
    public interface IGroupCallService
    {
        Task<GroupCallChangeDto> StartAsync(string userId, int groupId, bool video);
        Task<GroupCallChangeDto> JoinAsync(string userId, int callId);
        Task<GroupCallChangeDto?> LeaveAsync(string userId, int callId);
        Task<IEnumerable<GroupCallChangeDto>> LeaveAllForAsync(string userId);
        Task<GroupCallChangeDto?> SetScreenAsync(string userId, int callId, bool on);
        Task<GroupCallRingDto> RingAsync(string userId, int callId, string userName);
        Task<string> StopRingingAsync(string userId, int callId, string userName);
        Task<IEnumerable<string>> GetRingingAsync(string userId, int callId);
        Task DeclineAsync(string userId, int callId);
        Task<GroupCallDto?> GetMineAsync(string userId);
        Task<GroupCallDto?> GetActiveForGroupAsync(int groupId);
        Task<IEnumerable<GroupCallDto>> GetActiveInMyGroupsAsync(string userId);
        Task<bool> AreTogetherAsync(string userId, string otherUserId, int callId);
    }
}
