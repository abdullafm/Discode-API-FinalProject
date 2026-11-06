using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/reports")]
    [ApiController]
    [Authorize(Roles = Roles.Staff)]
    public class AdminReportController : ControllerBase
    {
        private readonly IAdminReportService _reports;

        public AdminReportController(IAdminReportService reports)
        {
            _reports = reports;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(await _reports.GetAsync(status, search, page, pageSize));
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetReportStatusDto model)
        {
            await _reports.SetStatusAsync(UserId, id, model);
            return Ok();
        }
    }
}
