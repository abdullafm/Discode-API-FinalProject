using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class JoinServerDtoValidator : AbstractValidator<JoinServerDto>
    {
        public JoinServerDtoValidator()
        {
            RuleFor(x => x.Invite)
                .Must(invite => !string.IsNullOrWhiteSpace(invite)).WithMessage("Enter an invite link.")
                .MaximumLength(300).WithMessage("That invite link is too long.");
        }
    }
}
