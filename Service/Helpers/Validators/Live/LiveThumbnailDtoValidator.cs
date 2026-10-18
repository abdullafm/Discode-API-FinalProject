using FluentValidation;
using Service.Helpers.DTOs.Live;
using System.Text.RegularExpressions;

namespace Service.Helpers.Validators.Live
{
    public class LiveThumbnailDtoValidator : AbstractValidator<LiveThumbnailDto>
    {
        private static readonly Regex Pattern = new("^/uploads/live/[0-9a-f]{32}\\.jpg$", RegexOptions.Compiled);

        public LiveThumbnailDtoValidator()
        {
            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("The preview image is missing.")
                .Must(url => Pattern.IsMatch(url)).WithMessage("The preview image is not valid.");
        }
    }
}
