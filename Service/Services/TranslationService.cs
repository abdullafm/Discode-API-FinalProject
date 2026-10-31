using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Translations;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class TranslationService : ITranslationService
    {
        public static readonly string[] Languages = { "en", "tr", "az", "ru" };

        private readonly ITranslationRepository _translations;
        private readonly IValidator<SetLanguageDto> _languageValidator;

        public TranslationService(ITranslationRepository translations, IValidator<SetLanguageDto> languageValidator)
        {
            _translations = translations;
            _languageValidator = languageValidator;
        }

        public async Task<TranslationSetDto> GetAsync(string? userId, string? language)
        {
            var code = language?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(code) || !Languages.Contains(code))
            {
                code = string.IsNullOrEmpty(userId) ? "en" : await _translations.GetUserLanguageAsync(userId);
                if (code is null || !Languages.Contains(code)) code = "en";
            }

            return new TranslationSetDto
            {
                Language = code,
                Entries = code == "en" ? new Dictionary<string, string>() : await _translations.GetByLanguageAsync(code)
            };
        }

        public async Task<TranslationSetDto> SetLanguageAsync(string userId, SetLanguageDto model)
        {
            await _languageValidator.EnsureValidAsync(model);
            await _translations.SetUserLanguageAsync(userId, model.Language);
            return await GetAsync(userId, model.Language);
        }
    }
}
