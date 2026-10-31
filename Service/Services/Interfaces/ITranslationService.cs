using Service.Helpers.DTOs.Translations;

namespace Service.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<TranslationSetDto> GetAsync(string? userId, string? language);
        Task<TranslationSetDto> SetLanguageAsync(string userId, SetLanguageDto model);
    }
}
