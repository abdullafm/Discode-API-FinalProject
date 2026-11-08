namespace Service.Services.Interfaces
{
    // The only place that talks to Stripe: the rest of the code (and the tests) use these plain records
    public interface IStripeGateway
    {
        Task<StripeCheckout> CreateCheckoutAsync(string userId, string email, string? customerId, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult?> GetCheckoutAsync(string sessionId);
        Task<StripeSubscriptionInfo?> GetSubscriptionAsync(string subscriptionId);
        Task<string> CreatePortalAsync(string customerId, string returnUrl);
        // stops the billing at once (used when an account is deleted)
        Task CancelSubscriptionAsync(string subscriptionId);
        // verifies the signature; null when it is not a valid Stripe delivery
        StripeEventInfo? ReadWebhook(string payload, string signature);
    }

    public record StripeCheckout(string Url, string SessionId);

    public record StripeCheckoutResult(string SessionId, bool Paid, string UserId, string CustomerId, string SubscriptionId);

    public record StripeSubscriptionInfo(string Id, string CustomerId, string UserId, string Status, DateTime PeriodEnd, bool CancelAtPeriodEnd);

    public record StripeEventInfo(string Id, string Type, string CustomerId, string SubscriptionId, string UserId, string Status,
                                  DateTime? PeriodEnd, bool CancelAtPeriodEnd, int? AmountCents, string Currency, string BillingReason);
}
