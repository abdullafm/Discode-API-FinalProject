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
        Task<GroupCallDto?> GetMineAsync(string userId);
        Task<IEnumerable<GroupCallDto>> GetActiveInMyGroupsAsync(string userId);
        Task<bool> AreTogetherAsync(string userId, string otherUserId, int callId);
    }
}
