using Domain.Common;

namespace Domain.Entities
{
    public class NitroGift : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public AppUser Buyer { get; set; } = null!;
        public int Days { get; set; }
        public int AmountCents { get; set; }
        public bool IsFree { get; set; }
        public string? StripeSessionId { get; set; }
        public string? RedeemedById { get; set; }
        public AppUser? RedeemedBy { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
