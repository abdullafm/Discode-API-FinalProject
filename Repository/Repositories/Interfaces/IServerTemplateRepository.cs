using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IServerTemplateRepository : IBaseRepository<ServerTemplate>
    {
        Task<ServerTemplate?> GetForServerAsync(int serverId);
        Task<ServerTemplate?> GetByCodeAsync(string code);
        Task<bool> CodeTakenAsync(string code);
    }
}
