using Domain.Common;

namespace Domain.Entities
{
    // One month of Nitro that somebody bought (or a super admin made) for another person; the link carries the code
    public class NitroGift : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        // who paid, or the super admin who made it
        public string BuyerId { get; set; } = string.Empty;
        public AppUser Buyer { get; set; } = null!;
        public int Days { get; set; }
        public int AmountCents { get; set; }
        // made by a super admin, nobody paid
        public bool IsFree { get; set; }
        // the Stripe payment that bought it: the same payment never makes two gifts
        public string? StripeSessionId { get; set; }
        public string? RedeemedById { get; set; }
        public AppUser? RedeemedBy { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
