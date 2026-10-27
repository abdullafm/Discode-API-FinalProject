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
        }
    }
}
