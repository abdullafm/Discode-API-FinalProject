using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LiveController : ControllerBase
    {
        private readonly ILiveStreamService _liveStreamService;

        public LiveController(ILiveStreamService liveStreamService)
        {
            _liveStreamService = liveStreamService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _liveStreamService.GetActiveAsync(UserId));
        }
    }
}
