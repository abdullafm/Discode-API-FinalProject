using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class UpdateServerProfileDtoValidator : AbstractValidator<UpdateServerProfileDto>
    {
        private static readonly string[] Banners = { "ink", "pink", "red", "orange", "yellow", "purple", "blue", "cyan", "green", "gray" };

        public UpdateServerProfileDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a server name.")
                .MaximumLength(100).WithMessage("The server name can be at most 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("The description can be at most 300 characters.");

            RuleFor(x => x.BannerColor)
                .Must(color => !string.IsNullOrWhiteSpace(color) && Banners.Contains(color.Trim().ToLowerInvariant())).WithMessage("Pick a valid banner color.");

            RuleFor(x => x.IconUrl)
                .Must(url => url is null || url.Length <= 200).WithMessage("That icon address is too long.");
        }
    }
}
