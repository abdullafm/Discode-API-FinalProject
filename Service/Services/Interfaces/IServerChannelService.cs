using Service.Helpers.DTOs.ServerChannels;

namespace Service.Services.Interfaces
{
    public interface IServerChannelService
    {
        Task EnsureDefaultsAsync(int serverId);
        Task<ServerChannelsDto> GetAsync(string userId, int serverId);
        Task<ServerChannelDto> CreateChannelAsync(string userId, int serverId, CreateServerChannelDto model);
        Task<ServerCategoryDto> CreateCategoryAsync(string userId, int serverId, CreateServerCategoryDto model);
        Task<ServerChannelDto> UpdateChannelAsync(string userId, int serverId, int channelId, UpdateServerChannelDto model);
        Task<ServerPermissionSetDto> GetChannelPermissionsAsync(string userId, int serverId, int channelId);
        Task<ServerPermissionSetDto> GetCategoryPermissionsAsync(string userId, int serverId, int categoryId);
        Task<ServerOverwriteTargetsDto> SearchTargetsAsync(string userId, int serverId, string? search);
        Task<List<string>> GetViewerIdsAsync(string userId, int serverId, int channelId);
        Task DeleteChannelAsync(string userId, int serverId, int channelId);
        Task<ServerCategoryDto> UpdateCategoryAsync(string userId, int serverId, int categoryId, UpdateServerCategoryDto model);
        Task DeleteCategoryAsync(string userId, int serverId, int categoryId);
        Task<List<string>> GetMemberIdsAsync(int serverId);
    }
}
