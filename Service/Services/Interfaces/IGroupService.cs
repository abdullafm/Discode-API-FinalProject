using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Groups;

namespace Service.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateAsync(string ownerId, CreateGroupDto model);
        Task<IEnumerable<GroupDto>> GetMineAsync(string userId);
        Task<GroupDto> GetAsync(string userId, int groupId);
        Task MarkReadAsync(string userId, int groupId);
        Task<IEnumerable<GroupMessageDto>> GetMessagesAsync(string userId, int groupId, int? beforeId = null);
        Task<GroupChangeDto> SendMessageAsync(string userId, int groupId, SendGroupMessageDto model);
        Task<GroupChangeDto> SendPollAsync(string userId, int groupId, CreatePollDto model);
        Task<GroupMessageDeletedDto> DeleteMessageAsync(string userId, int groupId, int messageId);
        Task<IEnumerable<SentMessageDto>> ForwardMessageAsync(string userId, int groupId, int messageId, ForwardMessageDto model);
        Task<GroupMessageUpdateDto> ToggleReactionAsync(string userId, int groupId, int messageId, ToggleReactionDto model);
        Task<GroupMessageUpdateDto> VotePollAsync(string userId, int groupId, int messageId, VotePollDto model);
        Task<GroupChangeDto> UpdateAsync(string userId, int groupId, UpdateGroupDto model);
        Task<GroupChangeDto> AddMembersAsync(string userId, int groupId, AddGroupMembersDto model);
        Task<GroupChangeDto> RemoveMemberAsync(string userId, int groupId, string userName);
        Task<GroupChangeDto> LeaveAsync(string userId, int groupId);
    }
}
