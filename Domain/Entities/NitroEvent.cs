using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class NitroEvent : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public NitroEventKind Kind { get; set; }
        public NitroSource Source { get; set; } = NitroSource.Stripe;
        public string Details { get; set; } = string.Empty;
        public int? AmountCents { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? StripeEventId { get; set; }
        public string? ActorId { get; set; }
    }
}
