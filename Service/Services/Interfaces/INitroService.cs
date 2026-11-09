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
        // an account was deleted: its Nitro ends, its boosts go and the Stripe billing stops
        Task<NitroChangeDto> EndForDeletedAccountAsync(string userId);
        Task<NitroChangeDto> GrantAsync(string actorId, GrantNitroDto model);
        Task<NitroChangeDto> RevokeAsync(string actorId, string userName);
        Task<AdminNitroPageDto> GetAdminPageAsync(string? search, bool activeOnly, int page);
        // longer or shorter wait before the boosts of a person can be moved
        Task<NitroChangeDto> AdjustBoostTimeAsync(string actorId, string userName, int hours);

        // ---- gifts
        Task<CheckoutDto> CreateGiftCheckoutAsync(string userId, CreateCheckoutDto model);
        Task<NitroGiftDto> ConfirmGiftAsync(string userId, ConfirmCheckoutDto model);
        Task<List<NitroGiftDto>> GetMyGiftsAsync(string userId);
        Task<NitroGiftInfoDto> GetGiftAsync(string viewerId, string code);
        Task<(NitroStatusDto Status, NitroChangeDto Change)> RedeemGiftAsync(string userId, string code);
        Task<NitroGiftDto> CreateFreeGiftAsync(string actorId, CreateFreeGiftDto model);
        Task RevokeGiftAsync(string actorId, int giftId);
        Task<AdminNitroGiftPageDto> GetAdminGiftsAsync(string? search, int page);
    }
}
