using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public interface IServerAuditLogRepository : IBaseRepository<ServerAuditLog>
    {
        Task<List<ServerAuditLog>> GetForServerAsync(int serverId, string? actorId, ServerAuditAction? action, int? beforeId, int take);
    }
}
