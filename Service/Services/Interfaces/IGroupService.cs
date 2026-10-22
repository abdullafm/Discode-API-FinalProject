using Service.Helpers.DTOs.Groups;

namespace Service.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateAsync(string ownerId, CreateGroupDto model);
        Task<IEnumerable<GroupDto>> GetMineAsync(string userId);
        Task<GroupDto> GetAsync(string userId, int groupId);
    }
}
