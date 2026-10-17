using FluentValidation;
using Service.Helpers.DTOs.Posts;

namespace Service.Helpers.Validators.Posts
{
    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.Text)
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Write a comment first.")
                .MaximumLength(280).WithMessage("A comment can be at most 280 characters.");
        }
    }
}
