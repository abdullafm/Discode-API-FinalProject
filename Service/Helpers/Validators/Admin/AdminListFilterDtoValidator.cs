using FluentValidation;
using Service.Helpers.DTOs.Admin;

namespace Service.Helpers.Validators.Admin
{
    public class AdminListFilterDtoValidator : AbstractValidator<AdminListFilterDto>
    {
        public AdminListFilterDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("The page must be 1 or higher.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Ask for between 1 and 100 rows.");

            RuleFor(x => x.Role)
                .Must(role => role is null || role == "Admin" || role == "SuperAdmin" || role == "Member")
                .WithMessage("That role is not supported.");

            RuleFor(x => x.Search)
                .Must(text => text is null || text.Length <= 100).WithMessage("That search is too long.");
        }
    }
}
