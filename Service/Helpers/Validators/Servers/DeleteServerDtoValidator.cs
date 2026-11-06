using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class DeleteServerDtoValidator : AbstractValidator<DeleteServerDto>
    {
        public DeleteServerDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Type the server name to confirm.");
        }
    }
}
