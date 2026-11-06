using Service.Helpers.DTOs.ServerPrune;

namespace Service.Services.Interfaces
{
    public interface IServerPruneService
    {
        Task<int> PreviewAsync(string userId, int serverId, ServerPruneDto model);
        Task<ServerPruneChangeDto> PruneAsync(string userId, int serverId, ServerPruneDto model);
    }
}
