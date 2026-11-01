using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BrandingService : IBrandingService
    {
        private readonly ISiteSettingRepository _settings;
        private readonly IUserModerationRepository _moderation;

        public BrandingService(ISiteSettingRepository settings, IUserModerationRepository moderation)
        {
            _settings = settings;
            _moderation = moderation;
        }

        public async Task<BrandingDto> GetAsync()
        {
            var rows = (await _settings.GetAllAsync()).ToDictionary(x => x.Key, x => x.Value);
            return new BrandingDto
            {
                LogoUrl = rows.GetValueOrDefault(BrandingSlots.Logo, string.Empty),
                FaviconUrl = rows.GetValueOrDefault(BrandingSlots.Favicon, string.Empty)
            };
        }

        public async Task<BrandingDto> SetAsync(string adminId, string slot, SetBrandingDto model)
        {
            EnsureSlot(slot);
            var url = (model.Url ?? string.Empty).Trim();
            if (url.Length == 0 || url.Length > 500 || !url.StartsWith("/uploads/branding/", StringComparison.Ordinal))
            {
                throw new BadRequestException("Choose an image to upload.");
            }

            await _settings.SetAsync(slot, url);
            await LogAsync(adminId, slot, "Changed");
            return await GetAsync();
        }

        public async Task<BrandingDto> ResetAsync(string adminId, string slot)
        {
            EnsureSlot(slot);
            await _settings.RemoveAsync(slot);
            await LogAsync(adminId, slot, "Back to the default");
            return await GetAsync();
        }

        private static void EnsureSlot(string slot)
        {
            if (!BrandingSlots.All.Contains(slot))
            {
                throw new NotFoundException("Unknown logo.");
            }
        }

        private async Task LogAsync(string adminId, string slot, string what)
        {
            await _moderation.AddActionAsync(new AdminAction
            {
                AdminId = adminId,
                TargetType = AdminTargetType.Site,
                TargetId = slot,
                TargetName = slot == BrandingSlots.Logo ? "Site logo" : "Favicon",
                Kind = AdminActionKind.BrandingChange,
                Reason = what
            });
            await _moderation.SaveChangesAsync();
        }
    }
}
