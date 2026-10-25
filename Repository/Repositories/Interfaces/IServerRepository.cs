using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerRepository : IBaseRepository<Server>
    {
        Task<Server?> GetWithMembersAsync(int id);
        Task<Server?> GetByInviteCodeAsync(string inviteCode);
        Task<IEnumerable<Server>> GetForUserAsync(string userId);
        Task<bool> InviteCodeExistsAsync(string inviteCode);
    }
}
