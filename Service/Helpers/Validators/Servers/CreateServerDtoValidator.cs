using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class CreateServerDtoValidator : AbstractValidator<CreateServerDto>
    {
        public CreateServerDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a server name.")
                .MaximumLength(100).WithMessage("The server name can be at most 100 characters.");
        }
    }
}
