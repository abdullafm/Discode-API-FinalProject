namespace Service.Helpers.DTOs.Nitro
{
    public class AdminNitroSubscriptionDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public int Boosts { get; set; }
        public long TotalPaidCents { get; set; }
        public int Payments { get; set; }
        public DateTime? LastPaymentAt { get; set; }
    }

    public class AdminNitroEventDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int? AmountCents { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
    }

    public class AdminNitroPageDto
    {
        public List<AdminNitroSubscriptionDto> Subscriptions { get; set; } = new();
        public List<AdminNitroEventDto> Events { get; set; } = new();
        public int ActiveCount { get; set; }
        public int TotalSubscriptions { get; set; }
        public int TotalEvents { get; set; }
        public long RevenueCents { get; set; }
        public long RevenueThisMonthCents { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
