using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerVoiceStateRepository : IBaseRepository<ServerVoiceState>
    {
        Task<ServerVoiceState?> GetForUserAsync(string userId);
        Task<List<ServerVoiceState>> GetForServerAsync(int serverId);
        Task<List<string>> GetAllUserIdsAsync();
        Task DeleteForUserInServerAsync(int serverId, string userId);
    }
}
