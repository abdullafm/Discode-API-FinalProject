using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        private const int MinAge = 13;

        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email.Trim())
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.")
                .MaximumLength(256);

            RuleFor(x => x.DisplayName.Trim())
                .NotEmpty().WithMessage("Enter a display name.")
                .MaximumLength(32).WithMessage("Display name can be at most 32 characters.");

            RuleFor(x => x.UserName.Trim())
                .NotEmpty().WithMessage("Enter a username.")
                .MinimumLength(2).WithMessage("Username must be at least 2 characters.")
                .MaximumLength(32).WithMessage("Username can be at most 32 characters.")
                .Matches("^[a-zA-Z0-9._]+$").WithMessage("Username can only contain letters, numbers, dots and underscores.");

            RuleFor(x => x.BirthDay)
                .NotEmpty().WithMessage("Enter your date of birth.")
                .Must(BeOldEnough).WithMessage($"You must be {MinAge} or older to create an account.");

            RuleFor(x => x.Password).MustBeStrongPassword();
        }

        private static bool BeOldEnough(DateTime birthDay)
        {
            var today = DateTime.UtcNow.Date;
            if (birthDay.Date > today) return false;

            var age = today.Year - birthDay.Year;
            if (birthDay.Date > today.AddYears(-age)) age--;
            return age >= MinAge;
        }
    }
}
