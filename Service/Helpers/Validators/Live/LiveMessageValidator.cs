using FluentValidation;

namespace Service.Helpers.Validators.Live
{
    public class LiveMessageValidator : AbstractValidator<string>
    {
        public LiveMessageValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("Write a message.")
                .MaximumLength(200).WithMessage("The message can be at most 200 characters.");
        }
    }
}
