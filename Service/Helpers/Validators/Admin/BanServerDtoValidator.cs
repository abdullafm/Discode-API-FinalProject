using FluentValidation;
using Service.Helpers.DTOs.Admin;

namespace Service.Helpers.Validators.Admin
{
    public class BanServerDtoValidator : AbstractValidator<BanServerDto>
    {
        public BanServerDtoValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Write the reason for the ban.")
                .MaximumLength(300).WithMessage("The reason can be at most 300 characters.");
        }
    }
}
