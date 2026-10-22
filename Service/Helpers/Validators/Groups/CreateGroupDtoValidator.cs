using FluentValidation;
using Service.Helpers.DTOs.Groups;

namespace Service.Helpers.Validators.Groups
{
    public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
    {
        public const int MaxMembers = 10;

        public CreateGroupDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("The group name can be at most 100 characters.");

            RuleFor(x => x.UserNames)
                .Must(names => names.Count >= 2).WithMessage("Pick at least two friends for a group.")
                .Must(names => names.Count <= MaxMembers - 1).WithMessage("A group can have up to " + MaxMembers + " members including you.")
                .Must(names => names.All(name => !string.IsNullOrWhiteSpace(name))).WithMessage("Pick friends from the list.");
        }
    }
}
