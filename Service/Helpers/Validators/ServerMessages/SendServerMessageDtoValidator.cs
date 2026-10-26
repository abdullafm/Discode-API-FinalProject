using FluentValidation;
using Service.Helpers.DTOs.ServerMessages;

namespace Service.Helpers.Validators.ServerMessages
{
    public class SendServerMessageDtoValidator : AbstractValidator<SendServerMessageDto>
    {
        public SendServerMessageDtoValidator()
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
}
