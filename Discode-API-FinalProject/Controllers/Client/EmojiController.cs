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
        private readonly IEmojiCategoryService _emojiCategoryService;

        public EmojiController(IEmojiCategoryService emojiCategoryService)
        {
            _emojiCategoryService = emojiCategoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _emojiCategoryService.GetAllAsync());
        }
    }
}
