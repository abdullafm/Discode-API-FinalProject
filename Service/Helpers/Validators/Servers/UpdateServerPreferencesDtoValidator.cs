using FluentValidation;
using Service.Helpers.DTOs.Servers;

namespace Service.Helpers.Validators.Servers
{
    public class UpdateServerPreferencesDtoValidator : AbstractValidator<UpdateServerPreferencesDto>
    {
        private static readonly string[] Levels = { "all", "mentions", "nothing" };

        public UpdateServerPreferencesDtoValidator()
        {
            RuleFor(x => x.MuteMinutes)
                .Must(minutes => minutes is null || (minutes >= -1 && minutes <= 525600)).WithMessage("Pick a valid mute time.");

            RuleFor(x => x.NotificationLevel)
                .Must(level => level is null || Levels.Contains(level.Trim().ToLowerInvariant())).WithMessage("Pick a valid notification setting.");
        }
    }
}
