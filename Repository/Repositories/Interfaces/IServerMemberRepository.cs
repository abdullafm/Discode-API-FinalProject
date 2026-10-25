using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerMemberRepository : IBaseRepository<ServerMember>
    {
        Task<ServerMember?> GetAsync(int serverId, string userId);
    }
}
