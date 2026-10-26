using FluentValidation;
using Service.Helpers.DTOs.ServerBans;

namespace Service.Helpers.Validators.ServerBans
{
    public class ServerBanFilterDtoValidator : AbstractValidator<ServerBanFilterDto>
    {
        public ServerBanFilterDtoValidator()
        {
            RuleFor(x => x.Take)
                .InclusiveBetween(1, 100).WithMessage("Ask for between 1 and 100 bans.");

            RuleFor(x => x.Search)
                .Must(text => text is null || text.Length <= 100).WithMessage("That search is too long.");
        }
    }
}
