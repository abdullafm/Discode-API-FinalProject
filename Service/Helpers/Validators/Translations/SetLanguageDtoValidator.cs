using FluentValidation;
using Service.Helpers.DTOs.Translations;
using Service.Services;

namespace Service.Helpers.Validators.Translations
{
    public class SetLanguageDtoValidator : AbstractValidator<SetLanguageDto>
    {
        public SetLanguageDtoValidator()
        {
            RuleFor(x => x.Language)
                .Must(code => TranslationService.Languages.Contains(code))
                .WithMessage("That language is not supported.");
        }
    }
}
