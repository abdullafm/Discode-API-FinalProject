using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IReportRepository _reports;
        private readonly IAdminRepository _admin;
        private readonly IUserModerationRepository _moderation;

        public AdminReportService(IReportRepository reports, IAdminRepository admin, IUserModerationRepository moderation)
        {
            _reports = reports;
            _admin = admin;
            _moderation = moderation;
        }

        public async Task<AdminReportListDto> GetAsync(string? status, string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            ReportStatus? filter = null;
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<ReportStatus>(status, true, out var parsed))
                {
                    throw new BadRequestException("Unknown report status.");
                }

                filter = parsed;
            }

            var (rows, total) = await _reports.GetPagedAsync(filter, search, page, pageSize);
            return new AdminReportListDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                OpenCount = await _admin.CountReportsByStatusAsync(ReportStatus.Open),
                ReviewedCount = await _admin.CountReportsByStatusAsync(ReportStatus.Reviewed),
                DismissedCount = await _admin.CountReportsByStatusAsync(ReportStatus.Dismissed),
                Items = rows.Select(x => new AdminReportDto
                {
                    Id = x.Report.Id,
                    CreatedAt = x.Report.CreatedAt,
                    Status = x.Report.Status.ToString(),
                    Reason = x.Report.Reason.ToString(),
                    Details = x.Report.Details,
                    TargetType = x.Report.TargetType.ToString(),
                    TargetId = x.Report.TargetId,
                    Snapshot = x.Report.ContentSnapshot,
                    ReporterId = x.Report.ReporterId,
                    ReporterUserName = x.ReporterUserName,
                    ReporterDisplayName = x.ReporterDisplayName,
                    ReporterAvatarUrl = x.ReporterAvatarUrl,
                    OwnerId = x.Report.TargetOwnerId,
                    OwnerUserName = x.OwnerUserName ?? string.Empty,
                    OwnerDisplayName = x.OwnerDisplayName ?? string.Empty,
                    OwnerAvatarUrl = x.OwnerAvatarUrl ?? string.Empty,
                    ServerId = x.Report.ContextServerId,
                    ServerName = x.ServerName ?? string.Empty,
                    GroupName = x.GroupName ?? string.Empty,
                    ReviewedBy = x.ReviewerUserName ?? string.Empty,
                    ReviewedAt = x.Report.ReviewedAt
                }).ToList()
            };
        }

        public async Task SetStatusAsync(string adminId, int reportId, SetReportStatusDto model)
        {
            if (!Enum.TryParse<ReportStatus>(model.Status, true, out var status))
            {
                throw new BadRequestException("Choose Open, Reviewed or Dismissed.");
            }

            var report = await _reports.GetByIdAsync(reportId) ?? throw new NotFoundException("Report not found.");
            if (report.Status == status)
            {
                throw new BadRequestException("The report already has that status.");
            }

            report.Status = status;
            report.ReviewedById = status == ReportStatus.Open ? null : adminId;
            report.ReviewedAt = status == ReportStatus.Open ? null : DateTime.UtcNow;
            await _reports.UpdateAsync(report);

            var kind = status switch
            {
                ReportStatus.Reviewed => AdminActionKind.ReportReviewed,
                ReportStatus.Dismissed => AdminActionKind.ReportDismissed,
                _ => AdminActionKind.ReportReopened
            };
            var ownerName = report.TargetOwnerId is null ? null : await _admin.GetUserNameAsync(report.TargetOwnerId);
            await _moderation.AddActionAsync(new AdminAction
            {
                AdminId = adminId,
                TargetType = AdminTargetType.User,
                TargetId = report.TargetOwnerId ?? string.Empty,
                TargetName = ownerName ?? "(unknown)",
                Kind = kind,
                Reason = "Report #" + report.Id + ": " + report.Reason,
                Details = report.TargetType.ToString()
            });
            await _moderation.SaveChangesAsync();
        }
    }
}
