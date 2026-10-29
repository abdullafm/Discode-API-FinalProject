using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerMemberRepository : IBaseRepository<ServerMember>
    {
        Task<ServerMember?> GetAsync(int serverId, string userId);
        Task<IEnumerable<ServerMember>> GetForUserAsync(string userId);
        Task<int> NextPositionAsync(string userId);
        Task<List<string>> GetUserIdsAsync(int serverId);
        Task<List<ServerMember>> GetByUserIdsAsync(int serverId, List<string> userIds);
        Task<List<ServerMember>> SearchAsync(int serverId, string? search, int take);
        Task<Dictionary<string, int>> AddMentionAsync(int serverId, List<string> userIds);
        Task ClearMentionsAsync(int serverId, string userId);
    }
}
