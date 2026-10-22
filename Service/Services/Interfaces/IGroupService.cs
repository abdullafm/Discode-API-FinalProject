using Service.Helpers.DTOs.Groups;

namespace Service.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateAsync(string ownerId, CreateGroupDto model);
        Task<IEnumerable<GroupDto>> GetMineAsync(string userId);
        Task<GroupDto> GetAsync(string userId, int groupId);
        Task<IEnumerable<GroupMessageDto>> GetMessagesAsync(string userId, int groupId);
        Task<GroupChangeDto> SendMessageAsync(string userId, int groupId, SendGroupMessageDto model);
        Task<GroupChangeDto> UpdateAsync(string userId, int groupId, UpdateGroupDto model);
        Task<GroupChangeDto> AddMembersAsync(string userId, int groupId, AddGroupMembersDto model);
        Task<GroupChangeDto> RemoveMemberAsync(string userId, int groupId, string userName);
        Task<GroupChangeDto> LeaveAsync(string userId, int groupId);
    }
}
