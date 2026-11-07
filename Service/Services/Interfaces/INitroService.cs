using Service.Helpers.DTOs.Nitro;

namespace Service.Services.Interfaces
{
    public interface INitroService
    {
        Task<NitroStatusDto> GetStatusAsync(string userId);
        Task<CheckoutDto> CreateCheckoutAsync(string userId, CreateCheckoutDto model);
        Task<CheckoutDto> CreatePortalAsync(string userId, CreatePortalDto model);
        Task<(NitroStatusDto Status, NitroChangeDto Change)> ConfirmCheckoutAsync(string userId, ConfirmCheckoutDto model);
        Task<NitroChangeDto> HandleWebhookAsync(string payload, string signature);
        // ends the Nitro of everybody whose paid time is over
        Task<NitroChangeDto> ExpireOverdueAsync();
        Task<NitroChangeDto> GrantAsync(string actorId, GrantNitroDto model);
        Task<NitroChangeDto> RevokeAsync(string actorId, string userName);
        Task<AdminNitroPageDto> GetAdminPageAsync(string? search, bool activeOnly, int page);
    }
}
