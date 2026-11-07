using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // The log the admin panel shows: who bought, renewed, cancelled, ran out, was given or lost Nitro
    public class NitroEvent : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public NitroEventKind Kind { get; set; }
        public NitroSource Source { get; set; } = NitroSource.Stripe;
        public string Details { get; set; } = string.Empty;
        public int? AmountCents { get; set; }
        public string Currency { get; set; } = string.Empty;
        // the Stripe event that caused it: a repeated delivery is recognised by it and ignored
        public string? StripeEventId { get; set; }
        // the admin who did it by hand
        public string? ActorId { get; set; }
    }
}
