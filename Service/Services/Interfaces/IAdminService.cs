using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
    }
}
