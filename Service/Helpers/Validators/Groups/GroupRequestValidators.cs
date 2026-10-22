using FluentValidation;
using Service.Helpers.DTOs.Groups;

namespace Service.Helpers.Validators.Groups
{
    public class SendGroupMessageDtoValidator : AbstractValidator<SendGroupMessageDto>
    {
        public SendGroupMessageDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.Attachments.Count > 0)
                .WithMessage("Write a message.");

            RuleFor(x => x.Text)
                .MaximumLength(2000).WithMessage("The message can be at most 2000 characters.");

            RuleFor(x => x.Attachments)
                .Must(list => list.Count <= 10).WithMessage("You can send at most 10 files at once.");

            RuleForEach(x => x.Attachments).ChildRules(file =>
            {
                file.RuleFor(a => a.Url)
                    .Must(BeUploadedFile).WithMessage("The file is not valid.");

                file.RuleFor(a => a.FileName)
                    .NotEmpty().WithMessage("The file has no name.")
                    .MaximumLength(200).WithMessage("The file name is too long.");

                file.RuleFor(a => a.ContentType)
                    .NotEmpty().WithMessage("The file type is missing.")
                    .MaximumLength(100).WithMessage("The file type is too long.");

                file.RuleFor(a => a.Size)
                    .InclusiveBetween(1, 25L * 1024 * 1024).WithMessage("A file can be at most 25 MB.");
            });
        }

        private static bool BeUploadedFile(string url)
        {
            const string folder = "/uploads/dm/";
            return !string.IsNullOrEmpty(url) &&
                   url.Length <= 200 &&
                   url.StartsWith(folder, StringComparison.Ordinal) &&
                   !url.Contains("..") &&
                   !url.Contains('\\') &&
                   url.IndexOf('/', folder.Length) == -1;
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
