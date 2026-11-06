using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces
{
    public interface IBrandingService
    {
        Task<BrandingDto> GetAsync();
        Task<BrandingDto> SetAsync(string adminId, string slot, SetBrandingDto model);
        Task<BrandingDto> ResetAsync(string adminId, string slot);
    }
}
