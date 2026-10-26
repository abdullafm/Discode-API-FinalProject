using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.ServerAudit;

namespace Service.Helpers.Validators.ServerAudit
{
    public class ServerAuditFilterDtoValidator : AbstractValidator<ServerAuditFilterDto>
    {
        public ServerAuditFilterDtoValidator()
        {
            RuleFor(x => x.Take)
                .InclusiveBetween(1, 100).WithMessage("Ask for between 1 and 100 entries.");

            RuleFor(x => x.Action)
                .Must(action => string.IsNullOrWhiteSpace(action) || Enum.TryParse<ServerAuditAction>(action, true, out _)).WithMessage("That action is not valid.");
        }
    }
}
