using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _admin;

        public AdminService(IAdminRepository admin)
        {
            _admin = admin;
        }

        public async Task<AdminOverviewDto> GetOverviewAsync()
        {
            return new AdminOverviewDto
            {
                Users = await _admin.CountUsersAsync(),
                Servers = await _admin.CountServersAsync(),
                OpenReports = await _admin.CountOpenReportsAsync(),
                Posts = await _admin.CountPostsAsync()
            };
        }
    }
}
