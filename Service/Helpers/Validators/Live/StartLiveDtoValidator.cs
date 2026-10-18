using FluentValidation;
using Service.Helpers.DTOs.Live;

namespace Service.Helpers.Validators.Live
{
    public class StartLiveDtoValidator : AbstractValidator<StartLiveDto>
    {
        public static readonly string[] Categories =
        {
            "Gaming", "Music", "Science & Tech", "Entertainment", "Education", "Student Hubs"
        };

        public StartLiveDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Give your stream a title.")
                .MaximumLength(70).WithMessage("The title can be at most 70 characters.");

            RuleFor(x => x.Category)
                .Must(category => Categories.Contains(category)).WithMessage("Choose a category.");
        }
    }
}
