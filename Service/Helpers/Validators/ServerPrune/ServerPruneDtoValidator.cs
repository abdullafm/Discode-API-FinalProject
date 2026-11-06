using FluentValidation;
using Service.Helpers.DTOs.ServerPrune;

namespace Service.Helpers.Validators.ServerPrune
{
    public class ServerPruneDtoValidator : AbstractValidator<ServerPruneDto>
    {
        public ServerPruneDtoValidator()
        {
            RuleFor(x => x.Days)
                .InclusiveBetween(1, 30).WithMessage("Choose between 1 and 30 days.");
        }
    }
}
