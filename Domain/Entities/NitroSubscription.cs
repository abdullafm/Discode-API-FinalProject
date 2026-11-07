using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // One row per person: whether they have Nitro right now and until when
    public class NitroSubscription : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public NitroSource Source { get; set; } = NitroSource.Stripe;
        public NitroStatus Status { get; set; } = NitroStatus.Active;
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime CurrentPeriodEnd { get; set; }
        // cancelled in Stripe: the paid month still runs until CurrentPeriodEnd
        public bool CancelAtPeriodEnd { get; set; }
        public DateTime? EndedAt { get; set; }

        public bool IsActive(DateTime now)
        {
            return Status != NitroStatus.Ended && CurrentPeriodEnd > now;
        }
    }
}
