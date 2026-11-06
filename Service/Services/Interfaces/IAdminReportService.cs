using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IAdminReportService
    {
        Task<AdminReportListDto> GetAsync(string? status, string? search, int page, int pageSize);
        Task SetStatusAsync(string adminId, int reportId, SetReportStatusDto model);
    }
}
