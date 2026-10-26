using Domain.Enums;

namespace Service.Services.Interfaces
{
    public interface IServerPermissionService
    {
        Task<ServerPermission> GetAsync(string userId, int serverId);
        Task EnsureAsync(string userId, int serverId, ServerPermission needed);
        Task EnsureEveryoneAsync(int serverId);
    }
}
