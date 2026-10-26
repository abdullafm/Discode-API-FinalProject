using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerMemberRepository : IBaseRepository<ServerMember>
    {
        Task<ServerMember?> GetAsync(int serverId, string userId);
        Task<IEnumerable<ServerMember>> GetForUserAsync(string userId);
        Task<int> NextPositionAsync(string userId);
        Task<List<string>> GetUserIdsAsync(int serverId);
    }
}
