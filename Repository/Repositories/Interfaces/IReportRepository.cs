using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public record ReportTarget(string OwnerId, string Snapshot, int? ServerId, int? GroupId);

    public interface IReportRepository : IBaseRepository<Report>
    {
        Task<ReportTarget?> ResolveAsync(string reporterId, ReportTargetType type, string targetId);
        Task<bool> HasOpenReportAsync(string reporterId, ReportTargetType type, string targetId);
    }
}
