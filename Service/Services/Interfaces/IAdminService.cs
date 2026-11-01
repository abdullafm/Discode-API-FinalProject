using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<AdminUserListDto> GetUsersAsync(AdminListFilterDto filter);
        Task<AdminServerListDto> GetAllServersAsync(AdminListFilterDto filter);
        Task<AdminServerListDto> SearchServersAsync(AdminListFilterDto filter);
        Task<AdminServerDetailDto> GetServerAsync(int serverId);
        Task<List<string>> BanServerAsync(string adminId, int serverId, BanServerDto model);
        Task<List<string>> UnbanServerAsync(int serverId);
        Task<List<string>> DeleteServerAsync(int serverId);
    }
}
