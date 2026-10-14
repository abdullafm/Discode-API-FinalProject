using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("This reset link is invalid or has expired.");

            RuleFor(x => x.NewPassword)
                .MustBeStrongPassword();

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("New passwords do not match.");
        }
    }
}
