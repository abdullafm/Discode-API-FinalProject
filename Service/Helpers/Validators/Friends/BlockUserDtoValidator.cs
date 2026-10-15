using FluentValidation;
using Service.Helpers.DTOs.Friends;

namespace Service.Helpers.Validators.Friends
{
    public class BlockUserDtoValidator : AbstractValidator<BlockUserDto>
    {
        public BlockUserDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.UserId) || !string.IsNullOrWhiteSpace(x.UserName))
                .WithMessage("Choose a user.");
        }
    }
}
