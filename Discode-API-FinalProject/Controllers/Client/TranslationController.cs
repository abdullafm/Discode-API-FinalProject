using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Translations;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? lang)
        {
            return Ok(await _translationService.GetAsync(UserId, lang));
        }

        [Authorize]
        [HttpPut("language")]
        public async Task<IActionResult> SetLanguage([FromBody] SetLanguageDto model)
        {
            return Ok(await _translationService.SetLanguageAsync(UserId!, model));
        }
    }
}
