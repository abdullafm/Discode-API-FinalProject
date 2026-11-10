using Service.Helpers.DTOs.ServerTemplates;
using Service.Helpers.DTOs.Servers;

namespace Service.Services.Interfaces
{
    public interface IServerTemplateService
    {
        Task<ServerTemplateDto?> GetAsync(string userId, int serverId);
        Task<ServerTemplateDto> SaveAsync(string userId, int serverId, SaveServerTemplateDto model);
        Task<ServerTemplateDto> SyncAsync(string userId, int serverId);
        Task DeleteAsync(string userId, int serverId);
        Task<ServerTemplateInfoDto> GetInfoAsync(string code);
        Task<ServerChangeDto> UseAsync(string userId, string code, UseServerTemplateDto model);
    }
}
