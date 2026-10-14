using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class ChangeUsernameDtoValidator : AbstractValidator<ChangeUsernameDto>
    {
        public ChangeUsernameDtoValidator()
        {
            RuleFor(x => x.UserName.Trim())
                .NotEmpty().WithMessage("Enter a username.")
                .MinimumLength(2).WithMessage("Username must be at least 2 characters.")
                .MaximumLength(32).WithMessage("Username can be at most 32 characters.")
                .Matches("^[a-zA-Z0-9._]+$").WithMessage("Username can only contain letters, numbers, dots and underscores.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Enter your current password.");
        }
    }
}
