using FluentValidation;
using Service.Helpers.DTOs.ServerChannels;

namespace Service.Helpers.Validators.ServerChannels
{
    public class CreateServerChannelDtoValidator : AbstractValidator<CreateServerChannelDto>
    {
        private static readonly string[] Kinds = { "text", "voice", "announce" };

        public CreateServerChannelDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a channel name.")
                .MaximumLength(100).WithMessage("The channel name can be at most 100 characters.");

            RuleFor(x => x.Kind)
                .Must(kind => !string.IsNullOrWhiteSpace(kind) && Kinds.Contains(kind.Trim().ToLowerInvariant())).WithMessage("Pick a valid channel type.");
        }
    }
}
