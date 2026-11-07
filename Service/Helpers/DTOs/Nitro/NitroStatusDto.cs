namespace Service.Helpers.DTOs.Nitro
{
    public class NitroStatusDto
    {
        public bool HasNitro { get; set; }
        // "stripe" or "admin"
        public string Source { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public bool CanManageBilling { get; set; }
        public bool IsBooster { get; set; }
        public int BoostSlots { get; set; }
        public int BoostsUsed { get; set; }
        public int MaxMessageLength { get; set; }
        public long MaxFileBytes { get; set; }
        public int PriceCents { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
