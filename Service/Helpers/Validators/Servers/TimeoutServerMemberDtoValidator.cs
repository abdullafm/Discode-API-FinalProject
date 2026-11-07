using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class TimeoutServerMemberDtoValidator : AbstractValidator<TimeoutServerMemberDto>
    {
        public TimeoutServerMemberDtoValidator()
        {
            RuleFor(x => x.Minutes)
                .InclusiveBetween(0, 28 * 24 * 60).WithMessage("A timeout can last at most 28 days.");

            RuleFor(x => x.Seconds)
                .InclusiveBetween(0, 28 * 24 * 60 * 60).WithMessage("A timeout can last at most 28 days.");

            RuleFor(x => x)
                .Must(x => (long)x.Minutes * 60 + x.Seconds <= 28L * 24 * 60 * 60).WithMessage("A timeout can last at most 28 days.");

            RuleFor(x => x.Reason)
                .MaximumLength(300).WithMessage("The reason can have at most 300 characters.");
        }
    }
}
