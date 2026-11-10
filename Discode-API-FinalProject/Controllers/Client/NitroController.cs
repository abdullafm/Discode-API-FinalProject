using Discode_API_FinalProject.Helpers;
using Discode_API_FinalProject.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Nitro;
using Service.Services.Interfaces;
using System.Security.Claims;
using System.Text;

namespace Discode_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NitroController : ControllerBase
    {
        private readonly INitroService _nitro;
        private readonly INitroAccessService _access;
        private readonly IBoostService _boosts;
        private readonly IHubContext<PresenceHub> _hubContext;

        public NitroController(INitroService nitro, INitroAccessService access, IBoostService boosts, IHubContext<PresenceHub> hubContext)
        {
            _nitro = nitro;
            _access = access;
            _boosts = boosts;
            _hubContext = hubContext;
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
            return Ok(await _nitro.GetStatusAsync(UserId));
        }

        [HttpGet("limits")]
        public async Task<IActionResult> Limits()
        {
            return Ok(await _access.GetLimitsAsync(UserId));
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateCheckoutDto model)
        {
            return Ok(await _nitro.CreateCheckoutAsync(UserId, model));
        }

        [HttpPost("portal")]
        public async Task<IActionResult> Portal([FromBody] CreatePortalDto model)
        {
            return Ok(await _nitro.CreatePortalAsync(UserId, model));
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmCheckoutDto model)
        {
            var result = await _nitro.ConfirmCheckoutAsync(UserId, model);
            await TellAsync(result.Change);
            return Ok(result.Status);
        }

        [HttpPost("gift/checkout")]
        public async Task<IActionResult> GiftCheckout([FromBody] CreateCheckoutDto model)
        {
            return Ok(await _nitro.CreateGiftCheckoutAsync(UserId, model));
        }

        [HttpPost("gift/confirm")]
        public async Task<IActionResult> GiftConfirm([FromBody] ConfirmCheckoutDto model)
        {
            return Ok(await _nitro.ConfirmGiftAsync(UserId, model));
        }

        [HttpGet("gifts")]
        public async Task<IActionResult> MyGifts()
        {
            return Ok(await _nitro.GetMyGiftsAsync(UserId));
        }

        [HttpGet("gift/{code}")]
        public async Task<IActionResult> GiftInfo(string code)
        {
            return Ok(await _nitro.GetGiftAsync(UserId, code));
        }

        [HttpPost("gift/{code}/redeem")]
        public async Task<IActionResult> Redeem(string code)
        {
            var result = await _nitro.RedeemGiftAsync(UserId, code);
            await TellAsync(result.Change);
            return Ok(result.Status);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();
            var change = await _nitro.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString());
            await TellAsync(change);
            return Ok();
        }

        private async Task TellAsync(NitroChangeDto change)
        {
            await NitroNotifier.TellAsync(_hubContext, _boosts, change);
        }
    }
}
