using Domain.Enums;
using Service.Helpers.DTOs.ServerAudit;

namespace Service.Services.Interfaces
{
    public interface IServerAuditService
    {
        Task LogAsync(int serverId, string actorId, ServerAuditAction action, string targetType, string targetId, string targetName, string details);
        Task<IEnumerable<ServerAuditEntryDto>> GetAsync(string userId, int serverId, ServerAuditFilterDto filter);
    }
}
