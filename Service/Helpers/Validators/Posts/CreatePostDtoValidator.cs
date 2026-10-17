using FluentValidation;
using Service.Helpers.DTOs.Posts;

namespace Service.Helpers.Validators.Posts
{
    public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
    {
        private const string ImageFolder = "/uploads/posts/";

        public CreatePostDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Text) || !string.IsNullOrWhiteSpace(x.ImageUrl))
                .WithMessage("Write something or add a photo.");

            RuleFor(x => x.Text)
                .MaximumLength(500).WithMessage("A post can be at most 500 characters.");

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
