using FluentValidation;
using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.Validators.DirectMessages
{
    public class SendMessageDtoValidator : AbstractValidator<SendMessageDto>
    {
        public SendMessageDtoValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Write a message.")
                .Must(text => text.Trim().Length > 0).WithMessage("Write a message.")
                .MaximumLength(2000).WithMessage("The message can be at most 2000 characters.");
        }
    }
}
