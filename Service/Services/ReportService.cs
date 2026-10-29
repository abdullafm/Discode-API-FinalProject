using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reports;
        private readonly IValidator<CreateReportDto> _validator;

        public ReportService(IReportRepository reports, IValidator<CreateReportDto> validator)
        {
            _reports = reports;
            _validator = validator;
        }

        public async Task CreateAsync(string reporterId, CreateReportDto model)
        {
            await _validator.EnsureValidAsync(model);

            var type = Enum.Parse<ReportTargetType>(model.TargetType, true);
            var reason = Enum.Parse<ReportReason>(model.Reason, true);
            var targetId = model.TargetId.Trim();

            var target = await _reports.ResolveAsync(reporterId, type, targetId)
                ?? throw new NotFoundException("This cannot be reported anymore.");

            if (target.OwnerId == reporterId)
            {
                throw new BadRequestException("You cannot report your own content.");
            }

            if (type == ReportTargetType.User)
            {
                targetId = target.OwnerId;
            }

            if (await _reports.HasOpenReportAsync(reporterId, type, targetId))
            {
                throw new BadRequestException("You already reported this. We will take a look.");
            }

            await _reports.AddAsync(new Report
            {
                ReporterId = reporterId,
                TargetType = type,
                TargetId = targetId,
                TargetOwnerId = target.OwnerId,
                Reason = reason,
                Details = (model.Details ?? string.Empty).Trim(),
                ContentSnapshot = target.Snapshot,
                ContextServerId = target.ServerId,
                ContextGroupId = target.GroupId
            });
        }
    }
}
