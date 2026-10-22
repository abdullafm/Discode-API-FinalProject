using FluentValidation;
using Service.Helpers.DTOs.Groups;

namespace Service.Helpers.Validators.Groups
{
    public class SendGroupMessageDtoValidator : AbstractValidator<SendGroupMessageDto>
    {
        public SendGroupMessageDtoValidator()
        {
            RuleFor(x => x.Text)
                .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Write a message.")
                .MaximumLength(2000).WithMessage("The message can be at most 2000 characters.");
        }
    }

    public class UpdateGroupDtoValidator : AbstractValidator<UpdateGroupDto>
    {
        private const string IconFolder = "/uploads/groups/";

        public UpdateGroupDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Give the group a name.")
                .MaximumLength(80).WithMessage("The group name can be at most 80 characters.");

            RuleFor(x => x.IconUrl)
                .Must(BeUploadedIcon).WithMessage("The group icon is not valid.")
                .When(x => !string.IsNullOrWhiteSpace(x.IconUrl));
        }

        private static bool BeUploadedIcon(string url)
        {
            return url.Length <= 200 &&
                   url.StartsWith(IconFolder, StringComparison.Ordinal) &&
                   !url.Contains("..") &&
                   !url.Contains('\\') &&
                   url.IndexOf('/', IconFolder.Length) == -1;
        }
    }

    public class AddGroupMembersDtoValidator : AbstractValidator<AddGroupMembersDto>
    {
        public AddGroupMembersDtoValidator()
        {
            RuleFor(x => x.UserNames)
                .Must(names => names.Count >= 1).WithMessage("Pick at least one friend.")
                .Must(names => names.Count <= CreateGroupDtoValidator.MaxMembers - 1).WithMessage("A group can have up to " + CreateGroupDtoValidator.MaxMembers + " members.")
                .Must(names => names.All(n => !string.IsNullOrWhiteSpace(n))).WithMessage("Pick friends from the list.");
        }
    }
}
