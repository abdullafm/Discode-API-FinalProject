using FluentValidation;
using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.Validators.DirectMessages
{
    public class ToggleReactionDtoValidator : AbstractValidator<ToggleReactionDto>
    {
        public ToggleReactionDtoValidator()
        {
            RuleFor(x => x.Emoji)
                .NotEmpty().WithMessage("Choose an emoji.")
                .Must(emoji => emoji.Trim().Length > 0).WithMessage("Choose an emoji.")
                .MaximumLength(32).WithMessage("That emoji is not supported.");
        }
    }
}
