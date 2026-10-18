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
        private readonly ILiveService _liveService;

        public LiveController(ILiveService liveService)
        {
            _liveService = liveService;
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
            return Ok(await _liveService.GetActiveAsync(UserId));
        }
    }
}
