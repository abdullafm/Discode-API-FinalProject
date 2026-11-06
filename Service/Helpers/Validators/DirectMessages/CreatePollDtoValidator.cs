using FluentValidation;
using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.Validators.DirectMessages
{
    public class CreatePollDtoValidator : AbstractValidator<CreatePollDto>
    {
        private static readonly int[] Durations = { 1, 4, 8, 24, 72, 168, 336 };

        public CreatePollDtoValidator()
        {
            RuleFor(x => x.Question)
                .Must(q => !string.IsNullOrWhiteSpace(q)).WithMessage("Ask a question.")
                .MaximumLength(300).WithMessage("The question can be at most 300 characters.");

            RuleFor(x => x.Options)
                .Must(o => o.Count >= 2).WithMessage("Add at least two answers.")
                .Must(o => o.Count <= 10).WithMessage("A poll can have at most 10 answers.");

            RuleForEach(x => x.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.Text)
                    .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Every answer needs some text.")
                    .MaximumLength(55).WithMessage("An answer can be at most 55 characters.");

                option.RuleFor(o => o.Emoji)
                    .MaximumLength(32).WithMessage("The emoji is not valid.");
            });

            RuleFor(x => x.DurationHours)
                .Must(h => Durations.Contains(h)).WithMessage("Choose how long the poll lasts.");
        }
    }
}
