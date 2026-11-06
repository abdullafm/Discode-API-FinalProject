using FluentValidation;
using Service.Helpers.DTOs.Stories;

namespace Service.Helpers.Validators.Stories
{
    public class CreateStoryDtoValidator : AbstractValidator<CreateStoryDto>
    {
        private const string ImageFolder = "/uploads/stories/";

        public CreateStoryDtoValidator()
        {
            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Add a photo to your story.");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(200).WithMessage("The photo link is too long.")
                .Must(BeUploadedImage).WithMessage("The photo is not valid.")
                .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
        }

        private static bool BeUploadedImage(string url)
        {
            return url.StartsWith(ImageFolder, StringComparison.Ordinal) &&
                   !url.Contains("..") &&
                   !url.Contains('\\') &&
                   url.IndexOf('/', ImageFolder.Length) == -1;
        }
    }
}
