using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class UpdateServerBannerDtoValidator : AbstractValidator<UpdateServerBannerDto>
    {
        public UpdateServerBannerDtoValidator()
        {
            RuleFor(x => x.BannerUrl)
                .Must(url => url is not null && url.Length <= 200).WithMessage("That banner address is too long.");
        }
    }
}
