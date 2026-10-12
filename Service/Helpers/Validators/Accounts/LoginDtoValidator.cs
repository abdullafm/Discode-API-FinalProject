using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email.Trim())
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.");

            RuleFor(x => x.Password)
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Enter your password.");
        }
    }
}
