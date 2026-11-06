using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerBanRepository : IBaseRepository<ServerBan>
    {
        Task<List<ServerBan>> GetForServerAsync(int serverId, string? search, int? beforeId, int take);
        Task<ServerBan?> GetAsync(int serverId, string userId);
        Task<bool> IsBannedAsync(int serverId, string userId);
    }
}
