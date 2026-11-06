using FluentValidation;
using Service.Helpers.DTOs.ServerChannels;

namespace Service.Helpers.Validators.ServerChannels
{
    public class UpdateServerCategoryDtoValidator : AbstractValidator<UpdateServerCategoryDto>
    {
        public UpdateServerCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a category name.")
                .MaximumLength(100).WithMessage("The category name can be at most 100 characters.");
        }
    }
}
