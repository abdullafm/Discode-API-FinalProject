using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.ServerRoles;

namespace Service.Helpers.Validators.ServerRoles
{
    public class UpdateServerRoleDtoValidator : AbstractValidator<UpdateServerRoleDto>
    {
        private static readonly string[] Colors = { "gray", "pink", "red", "orange", "yellow", "purple", "blue", "cyan", "green" };

        public UpdateServerRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a role name.")
                .MaximumLength(100).WithMessage("The role name can be at most 100 characters.");

            RuleFor(x => x.Color)
                .Must(color => !string.IsNullOrWhiteSpace(color) && Colors.Contains(color.Trim().ToLowerInvariant())).WithMessage("Pick a valid role color.");

            RuleFor(x => x.Permissions)
                .Must(bits => bits >= 0 && (bits & ~(long)ServerPermission.All) == 0).WithMessage("Those permissions are not valid.");
        }
    }
}
