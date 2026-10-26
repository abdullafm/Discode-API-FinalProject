using Domain.Entities;
using Domain.Enums;
using Service.Helpers;

namespace Service.Services.Interfaces
{
    public interface IServerPermissionService
    {
        Task<ServerPermission> GetAsync(string userId, int serverId);
        Task EnsureAsync(string userId, int serverId, ServerPermission needed);
        Task EnsureEveryoneAsync(int serverId);
        Task<bool> CanViewChannelAsync(string userId, int serverId, ServerChannel channel);
        Task<List<string>> GetChannelViewerIdsAsync(int serverId, ServerChannel channel);
        Task<ServerPermission> GetInChannelAsync(string userId, int serverId, ServerChannel channel);
        Task<ChannelPermissionContext> GetContextAsync(string userId, int serverId, List<ServerOverwrite>? overwrites = null);
    }
}
