using FluentValidation;
using Service.Helpers.DTOs.ServerBans;

namespace Service.Helpers.Validators.ServerBans
{
    public class BanServerMemberDtoValidator : AbstractValidator<BanServerMemberDto>
    {
        public BanServerMemberDtoValidator()
        {
            RuleFor(x => x.UserId)
                .Must(id => !string.IsNullOrWhiteSpace(id)).WithMessage("Choose who to ban.");

            RuleFor(x => x.Reason)
                .Must(reason => reason is null || reason.Length <= 300).WithMessage("The reason can be at most 300 characters.");
        }
    }
}
