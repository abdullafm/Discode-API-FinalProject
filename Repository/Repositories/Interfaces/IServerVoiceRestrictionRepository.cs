using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerVoiceRestrictionRepository : IBaseRepository<ServerVoiceRestriction>
    {
        Task<ServerVoiceRestriction?> GetAsync(int serverId, string userId);
        Task<List<ServerVoiceRestriction>> GetForServerAsync(int serverId);
    }
}
