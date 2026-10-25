using Service.Helpers.DTOs.Servers;

namespace Service.Services.Interfaces
{
    public interface IServerService
    {
        Task<ServerChangeDto> CreateAsync(string userId, CreateServerDto model);
        Task<ServerChangeDto> JoinAsync(string userId, JoinServerDto model);
        Task<IEnumerable<ServerDto>> GetMineAsync(string userId);
        Task<ServerDto> GetAsync(string userId, int serverId);
    }
}
