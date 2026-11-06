using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class ReorderServersDtoValidator : AbstractValidator<ReorderServersDto>
    {
        public ReorderServersDtoValidator()
        {
            RuleFor(x => x.ServerIds)
                .Must(ids => ids is not null && ids.Count <= 200).WithMessage("That list of servers is too long.");
        }
    }
}
