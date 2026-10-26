using FluentValidation;
using Service.Helpers.DTOs.ServerRoles;

namespace Service.Helpers.Validators.ServerRoles
{
    public class CreateServerRoleDtoValidator : AbstractValidator<CreateServerRoleDto>
    {
        public CreateServerRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a role name.")
                .MaximumLength(100).WithMessage("The role name can be at most 100 characters.");
        }
    }
}
