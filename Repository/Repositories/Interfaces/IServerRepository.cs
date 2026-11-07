using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerRepository : IBaseRepository<Server>
    {
        Task<Server?> GetWithMembersAsync(int id);
        Task<Server?> GetByInviteCodeAsync(string inviteCode);
        Task<IEnumerable<Server>> GetForUserAsync(string userId);
        Task<bool> InviteCodeExistsAsync(string inviteCode);
        // is the word used as an invite code or a custom link by another server
        Task<bool> LinkTakenAsync(string link, int exceptServerId);
        Task<bool> IsBannedAsync(int serverId);
    }
}
