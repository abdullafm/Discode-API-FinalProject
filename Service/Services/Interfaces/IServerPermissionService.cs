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
        Task<int> GetTopPositionAsync(string userId, int serverId);
        Task EnsureCanActOnAsync(string actorId, int serverId, string targetId, string verb);
        Task EnsureRoleBelowAsync(string actorId, int serverId, ServerRole role);
        Task<List<string>> GetChannelViewerIdsAsync(int serverId, ServerChannel channel);
        Task<ServerPermission> GetInChannelAsync(string userId, int serverId, ServerChannel channel);
        Task<ChannelPermissionContext> GetContextAsync(string userId, int serverId, List<ServerOverwrite>? overwrites = null);
    }
}
