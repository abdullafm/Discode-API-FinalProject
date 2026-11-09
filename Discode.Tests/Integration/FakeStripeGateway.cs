using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    // Stands in for Stripe: the tests say what Stripe would answer
    public class FakeStripeGateway : IStripeGateway
    {
        public Dictionary<string, StripeCheckoutResult> Checkouts { get; } = new();
        public Dictionary<string, StripeSubscriptionInfo> Subscriptions { get; } = new();
        public string? LastCheckoutUser { get; private set; }
        public string? LastSuccessUrl { get; private set; }
        public string? LastPortalCustomer { get; private set; }
        // what ReadWebhook returns for a payload (the payload text is the key)
        public Dictionary<string, StripeEventInfo> Webhooks { get; } = new();

        public Task<StripeCheckout> CreateCheckoutAsync(string userId, string email, string? customerId, string successUrl, string cancelUrl)
        {
            LastCheckoutUser = userId;
            LastSuccessUrl = successUrl;
            return Task.FromResult(new StripeCheckout("https://checkout.stripe.test/pay/cs_" + userId, "cs_" + userId));
        }

        public Task<StripeCheckout> CreateGiftCheckoutAsync(string userId, string email, string successUrl, string cancelUrl)
        {
            LastCheckoutUser = userId;
            return Task.FromResult(new StripeCheckout("https://checkout.stripe.test/gift/cs_gift_" + userId, "cs_gift_" + userId));
        }

        public Task<StripeCheckoutResult?> GetCheckoutAsync(string sessionId)
        {
            return Task.FromResult(Checkouts.TryGetValue(sessionId, out var found) ? found : null);
        }

        public Task<StripeSubscriptionInfo?> GetSubscriptionAsync(string subscriptionId)
        {
            return Task.FromResult(Subscriptions.TryGetValue(subscriptionId, out var found) ? found : null);
        }

        public List<string> Cancelled { get; } = new();

        public Task CancelSubscriptionAsync(string subscriptionId)
        {
            Cancelled.Add(subscriptionId);
            return Task.CompletedTask;
        }

        public Task<string> CreatePortalAsync(string customerId, string returnUrl)
        {
            LastPortalCustomer = customerId;
            return Task.FromResult("https://billing.stripe.test/portal/" + customerId);
        }

        public StripeEventInfo? ReadWebhook(string payload, string signature)
        {
            return signature == "good" && Webhooks.TryGetValue(payload, out var found) ? found : null;
        }
    }
}
