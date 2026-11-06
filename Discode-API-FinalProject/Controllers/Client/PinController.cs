using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Pins;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PinController : ControllerBase
    {
        private readonly IConversationPinService _pins;

        public PinController(IConversationPinService pins)
        {
            _pins = pins;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _pins.GetAsync(UserId));
        }

        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] TogglePinDto model)
        {
            return Ok(await _pins.ToggleAsync(UserId, model?.Key ?? string.Empty));
        }
    }
}
