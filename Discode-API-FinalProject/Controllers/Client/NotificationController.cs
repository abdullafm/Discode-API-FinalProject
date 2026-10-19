using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _notificationService.GetAsync(UserId));
        }

        [HttpPost("read")]
        public async Task<IActionResult> ReadAll()
        {
            await _notificationService.MarkAllReadAsync(UserId);
            return Ok();
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> Read(int id)
        {
            await _notificationService.MarkReadAsync(UserId, id);
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteAsync(UserId, id);
            return Ok();
        }
    }
}
