using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Friends;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BlockController : ControllerBase
    {
        private readonly IBlockService _blockService;

        public BlockController(IBlockService blockService)
        {
            _blockService = blockService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Block([FromBody] BlockUserDto model)
        {
            return Ok(await _blockService.BlockAsync(UserId, model));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _blockService.GetAllAsync(UserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Unblock(int id)
        {
            await _blockService.UnblockAsync(UserId, id);
            return Ok();
        }
    }
}
