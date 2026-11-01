using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.GetUsersAsync(filter));
        }

        [HttpGet("servers")]
        public async Task<IActionResult> GetAllServers([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.GetAllServersAsync(filter));
        }

        [HttpGet("servers/search")]
        public async Task<IActionResult> SearchServers([FromQuery] Service.Helpers.DTOs.Admin.AdminListFilterDto filter)
        {
            return Ok(await _adminService.SearchServersAsync(filter));
        }

        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            return Ok(await _adminService.GetOverviewAsync());
        }
    }
}
