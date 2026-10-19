using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmojiController : ControllerBase
    {
        private readonly IEmojiService _emojiService;

        public EmojiController(IEmojiService emojiService)
        {
            _emojiService = emojiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _emojiService.GetAllAsync());
        }
    }
}
