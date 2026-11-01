using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<AdminUserListDto> GetUsersAsync(AdminListFilterDto filter);
        Task<AdminServerListDto> GetServersAsync(AdminListFilterDto filter);
    }
}
