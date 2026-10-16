using Domain.Enums;
using Service.Helpers.Exceptions;

namespace Service.Helpers.Mappings
{
    public static class PresenceStatusMap
    {
        public static PresenceStatus Parse(string? status)
        {
            return status?.Trim().ToLowerInvariant() switch
            {
                "online" => PresenceStatus.Online,
                "idle" => PresenceStatus.Idle,
                "dnd" => PresenceStatus.DoNotDisturb,
                "invisible" => PresenceStatus.Invisible,
                _ => throw new BadRequestException("Choose a valid status.")
            };
        }

        public static string ToWire(PresenceStatus status)
        {
            return status switch
            {
                PresenceStatus.Idle => "idle",
                PresenceStatus.DoNotDisturb => "dnd",
                PresenceStatus.Invisible => "invisible",
                _ => "online"
            };
        }
    }
}
