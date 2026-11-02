using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public record ReportTarget(string OwnerId, string Snapshot, int? ServerId, int? GroupId);

    public record ReportRow(
        Report Report,
        string ReporterUserName, string ReporterDisplayName, string ReporterAvatarUrl,
        string? OwnerUserName, string? OwnerDisplayName, string? OwnerAvatarUrl,
        string? ServerName, string? GroupName, string? ReviewerUserName);

    public interface IReportRepository : IBaseRepository<Report>
    {
        Task<ReportTarget?> ResolveAsync(string reporterId, ReportTargetType type, string targetId);
        Task<(List<ReportRow> Items, int Total)> GetPagedAsync(ReportStatus? status, string? search, int page, int pageSize);
        Task<bool> HasOpenReportAsync(string reporterId, ReportTargetType type, string targetId);
    }
}
