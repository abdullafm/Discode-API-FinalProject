namespace Service.Services.Interfaces
{
    public interface IStripeGateway
    {
        Task<StripeCheckout> CreateCheckoutAsync(string userId, string email, string? customerId, string successUrl, string cancelUrl);
        Task<StripeCheckout> CreateGiftCheckoutAsync(string userId, string email, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult?> GetCheckoutAsync(string sessionId);
        Task<StripeSubscriptionInfo?> GetSubscriptionAsync(string subscriptionId);
        Task<string> CreatePortalAsync(string customerId, string returnUrl);
        Task CancelSubscriptionAsync(string subscriptionId);
        StripeEventInfo? ReadWebhook(string payload, string signature);
    }

    public record StripeCheckout(string Url, string SessionId);

    public record StripeCheckoutResult(string SessionId, bool Paid, string UserId, string CustomerId, string SubscriptionId, string Kind = "", int AmountCents = 0);

    public record StripeSubscriptionInfo(string Id, string CustomerId, string UserId, string Status, DateTime PeriodEnd, bool CancelAtPeriodEnd);

    public record StripeEventInfo(string Id, string Type, string CustomerId, string SubscriptionId, string UserId, string Status,
                                  DateTime? PeriodEnd, bool CancelAtPeriodEnd, int? AmountCents, string Currency, string BillingReason,
                                  string Kind = "", string SessionId = "");
}
