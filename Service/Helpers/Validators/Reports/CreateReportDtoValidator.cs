using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.Reports;

namespace Service.Helpers.Validators.Reports
{
    public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
    {
        public CreateReportDtoValidator()
        {
            RuleFor(x => x.TargetType)
                .Must(value => Enum.TryParse<ReportTargetType>(value, true, out _))
                .WithMessage("Choose what you are reporting.");

            RuleFor(x => x.TargetId)
                .NotEmpty().WithMessage("Choose what you are reporting.")
                .MaximumLength(450).WithMessage("Choose what you are reporting.");

            RuleFor(x => x.Reason)
                .Must(value => Enum.TryParse<ReportReason>(value, true, out _))
                .WithMessage("Choose a reason for the report.");

            RuleFor(x => x.Details)
                .MaximumLength(500).WithMessage("The details can be at most 500 characters.");
        }
    }
}
