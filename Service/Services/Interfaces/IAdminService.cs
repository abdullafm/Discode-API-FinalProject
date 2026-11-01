using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<AdminUserListDto> GetUsersAsync(AdminListFilterDto filter);
        Task<AdminServerListDto> GetAllServersAsync(AdminListFilterDto filter);
        Task<AdminServerListDto> SearchServersAsync(AdminListFilterDto filter);
    }
}
