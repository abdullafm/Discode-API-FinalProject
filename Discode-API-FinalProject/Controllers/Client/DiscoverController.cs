using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Discover;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiscoverController : ControllerBase
    {
        private readonly IDiscoverService _discoverService;

        public DiscoverController(IDiscoverService discoverService)
        {
            _discoverService = discoverService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DiscoverFilterDto filter)
        {
            return Ok(await _discoverService.GetAsync(filter));
        }
    }
}
