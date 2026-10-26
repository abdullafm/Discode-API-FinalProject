using Domain.Entities;
using Domain.Enums;

namespace Service.Services.Interfaces
{
    public interface IServerPermissionService
    {
        Task<ServerPermission> GetAsync(string userId, int serverId);
        Task EnsureAsync(string userId, int serverId, ServerPermission needed);
        Task EnsureEveryoneAsync(int serverId);
        Task<bool> CanViewChannelAsync(string userId, int serverId, ServerChannel channel);
    }
}
