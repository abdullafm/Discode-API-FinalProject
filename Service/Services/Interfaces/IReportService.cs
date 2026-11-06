using Service.Helpers.DTOs.Reports;

namespace Service.Services.Interfaces
{
    public interface IReportService
    {
        Task CreateAsync(string reporterId, CreateReportDto model);
    }
}
