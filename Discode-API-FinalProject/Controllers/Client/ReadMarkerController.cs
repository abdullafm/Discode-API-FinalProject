using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.ReadMarkers;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/readmarker")]
    [ApiController]
    [Authorize]
    public class ReadMarkerController : ControllerBase
    {
        private readonly IChatReadMarkerService _markers;

        public ReadMarkerController(IChatReadMarkerService markers)
        {
            _markers = markers;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string kind, [FromQuery] string key)
        {
            return Ok(await _markers.GetAsync(UserId, kind, key));
        }

        [HttpPut]
        public async Task<IActionResult> Mark([FromBody] MarkReadDto model)
        {
            return Ok(await _markers.MarkAsync(UserId, model));
        }
    }
}
