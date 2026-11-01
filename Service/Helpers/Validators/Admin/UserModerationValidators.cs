using FluentValidation;
using Service.Helpers.DTOs.Admin;

namespace Service.Helpers.Validators.Admin
{
    internal static class ReasonRules
    {
        public static IRuleBuilderOptions<T, string> Reason<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage("Write the reason.")
                .MaximumLength(300).WithMessage("The reason can be at most 300 characters.");
        }
    }

    public class WarnUserDtoValidator : AbstractValidator<WarnUserDto>
    {
        public WarnUserDtoValidator()
        {
            RuleFor(x => x.Reason).Reason();
        }
    }

    public class KickUserDtoValidator : AbstractValidator<KickUserDto>
    {
        public KickUserDtoValidator()
        {
            RuleFor(x => x.Reason).Reason();
        }
    }

    public class TimeoutUserDtoValidator : AbstractValidator<TimeoutUserDto>
    {
        public static readonly int[] AllowedMinutes = { 10, 60, 1440, 10080 };

        public TimeoutUserDtoValidator()
        {
            RuleFor(x => x.Reason).Reason();
            RuleFor(x => x.Minutes)
                .Must(minutes => AllowedMinutes.Contains(minutes))
                .WithMessage("Choose 10 minutes, 1 hour, 1 day or 7 days.");
        }
    }

    public class BanUserDtoValidator : AbstractValidator<BanUserDto>
    {
        public static readonly int[] AllowedDays = { 0, 1, 7, 30 };

        public BanUserDtoValidator()
        {
            RuleFor(x => x.Reason).Reason();
            RuleFor(x => x.Days)
                .Must(days => AllowedDays.Contains(days))
                .WithMessage("Choose 1 day, 7 days, 30 days or permanent.");
        }
    }

    public class DeleteUserDtoValidator : AbstractValidator<DeleteUserDto>
    {
        public DeleteUserDtoValidator()
        {
            RuleFor(x => x.Reason).Reason();
            RuleFor(x => x.UserName).NotEmpty().WithMessage("Type the username to confirm.");
        }
    }
}
