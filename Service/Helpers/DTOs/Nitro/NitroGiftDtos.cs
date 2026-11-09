namespace Service.Helpers.DTOs.Nitro
{
    public class NitroGiftDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Days { get; set; }
        public string Status { get; set; } = "available";
        public DateTime CreatedAt { get; set; }
        public string RedeemedByUserName { get; set; } = string.Empty;
        public DateTime? RedeemedAt { get; set; }
    }

    public class NitroGiftInfoDto
    {
        public string Code { get; set; } = string.Empty;
        public string FromUserName { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = string.Empty;
        public string FromAvatarUrl { get; set; } = string.Empty;
        public int Days { get; set; }
        public string Status { get; set; } = "available";
        public bool CanRedeem { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class RedeemGiftDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class CreateFreeGiftDto
    {
        public int Days { get; set; }
    }

    public class AdminNitroGiftDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string BuyerUserName { get; set; } = string.Empty;
        public int Days { get; set; }
        public int AmountCents { get; set; }
        public bool IsFree { get; set; }
        public string Status { get; set; } = "available";
        public string RedeemedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
    }

    public class AdminNitroGiftPageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<AdminNitroGiftDto> Items { get; set; } = new();
    }
}
