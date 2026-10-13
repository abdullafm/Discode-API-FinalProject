using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
    {
        public VerifyEmailDtoValidator()
        {
            RuleFor(x => x.Email.Trim())
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.");

            RuleFor(x => x.Code.Trim())
                .NotEmpty().WithMessage("Enter the 4-digit code.")
                .Matches("^[0-9]{4}$").WithMessage("The code must be 4 digits.");
        }
    }
}
