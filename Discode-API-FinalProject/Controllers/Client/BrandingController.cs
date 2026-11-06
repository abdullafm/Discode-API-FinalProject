using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/branding")]
    [ApiController]
    public class BrandingController : ControllerBase
    {
        private readonly IBrandingService _branding;

        public BrandingController(IBrandingService branding)
        {
            _branding = branding;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _branding.GetAsync());
        }
    }
}
