using FluentValidation;
using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.Validators.DirectMessages
{
    public class ForwardMessageDtoValidator : AbstractValidator<ForwardMessageDto>
    {
        public const int MaxTargets = 10;

        public ForwardMessageDtoValidator()
        {
            RuleFor(x => x.UserNames)
                .NotEmpty().WithMessage("Choose at least one person.")
                .When(x => x.GroupIds.Count == 0);
            RuleFor(x => x.UserNames)
                .Must(names => names.Count <= MaxTargets).WithMessage("You can forward to at most 10 people at once.")
                .Must(names => names.All(name => !string.IsNullOrWhiteSpace(name))).WithMessage("Choose at least one person.");
            RuleFor(x => x.GroupIds)
                .Must(ids => ids.Count <= MaxTargets).WithMessage("You can forward to at most 10 groups at once.");

            RuleFor(x => x.Note)
                .MaximumLength(Domain.Common.NitroRules.NitroMessageLength).WithMessage("The message is too long.");
        }
    }
}
