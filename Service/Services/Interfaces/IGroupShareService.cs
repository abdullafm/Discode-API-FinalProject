using Service.Helpers.DTOs.Groups;

namespace Service.Services.Interfaces
{
    public interface IGroupShareService
    {
        Task<List<GroupChangeDto>> SendTextAsync(string userId, IEnumerable<int>? groupIds, string text);
        Task<List<GroupChangeDto>> ForwardFromDmAsync(string userId, int messageId, IEnumerable<int>? groupIds, string note);
        Task<List<GroupChangeDto>> ForwardFromGroupAsync(string userId, int groupId, int messageId, IEnumerable<int>? groupIds, string note);
    }
}
