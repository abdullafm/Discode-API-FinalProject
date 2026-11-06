using FluentValidation;
using Service.Helpers.DTOs.Discover;

namespace Service.Helpers.Validators.Discover
{
    public class DiscoverFilterDtoValidator : AbstractValidator<DiscoverFilterDto>
    {
        public DiscoverFilterDtoValidator()
        {
            RuleFor(x => x.Take)
                .InclusiveBetween(1, 100).WithMessage("Ask for between 1 and 100 communities.");

            RuleFor(x => x.Search)
                .Must(text => text is null || text.Length <= 100).WithMessage("That search is too long.");
        }
    }
}
