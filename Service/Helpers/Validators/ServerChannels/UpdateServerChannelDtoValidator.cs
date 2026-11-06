using FluentValidation;
using Service.Helpers.DTOs.ServerChannels;

namespace Service.Helpers.Validators.ServerChannels
{
    public class UpdateServerChannelDtoValidator : AbstractValidator<UpdateServerChannelDto>
    {
        public UpdateServerChannelDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a channel name.")
                .MaximumLength(100).WithMessage("The channel name can be at most 100 characters.");

            RuleFor(x => x.UserLimit)
                .InclusiveBetween(0, 99).When(x => x.UserLimit.HasValue).WithMessage("The user limit must be between 0 and 99.");
        }
    }
}
