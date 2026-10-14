using FluentValidation;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Validators;

namespace Service.Helpers.Validators.Accounts
{
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Enter your current password.");

            RuleFor(x => x.NewPassword)
                .MustBeStrongPassword()
                .NotEqual(x => x.CurrentPassword).WithMessage("Choose a different password.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("New passwords do not match.");
        }
    }
}
