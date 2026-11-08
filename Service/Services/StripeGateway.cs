using Domain.Common;
using Microsoft.Extensions.Options;
using Service.Helpers.Exceptions;
using Service.Helpers.Settings;
using Service.Services.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Service.Services
{
    public class StripeGateway : IStripeGateway
    {
        private readonly StripeSettings _settings;

        public StripeGateway(IOptions<StripeSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<StripeCheckout> CreateCheckoutAsync(string userId, string email, string? customerId, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                ClientReferenceId = userId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string> { ["userId"] = userId },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string> { ["userId"] = userId }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = NitroRules.Currency,
                            UnitAmount = NitroRules.MonthlyPriceCents,
                            Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = "month" },
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Discode Nitro",
                                Description = "Longer messages, bigger files, animated avatar and banner, 2 server boosts"
                            }
                        }
                    }
                }
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                options.Customer = customerId;
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                options.CustomerEmail = email;
            }

            var session = await Run(() => new SessionService(Client()).CreateAsync(options));
            return new StripeCheckout(session.Url ?? string.Empty, session.Id);
        }

        public async Task<StripeCheckoutResult?> GetCheckoutAsync(string sessionId)
        {
            try
            {
                var session = await new SessionService(Client()).GetAsync(sessionId);
                var paid = session.Status == "complete" && (session.PaymentStatus == "paid" || session.PaymentStatus == "no_payment_required");
                return new StripeCheckoutResult(session.Id, paid, session.ClientReferenceId ?? string.Empty, session.CustomerId ?? string.Empty, session.SubscriptionId ?? string.Empty);
            }
            catch (StripeException failure) when (failure.StripeError?.Code == "resource_missing" || failure.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (StripeException failure)
            {
                throw Wrap(failure);
            }
        }

        public async Task<StripeSubscriptionInfo?> GetSubscriptionAsync(string subscriptionId)
        {
            try
            {
                var subscription = await new SubscriptionService(Client()).GetAsync(subscriptionId);
                return ToInfo(subscription);
            }
            catch (StripeException failure) when (failure.StripeError?.Code == "resource_missing" || failure.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (StripeException failure)
            {
                throw Wrap(failure);
            }
        }

        public async Task<string> CreatePortalAsync(string customerId, string returnUrl)
        {
            var session = await Run(() => new Stripe.BillingPortal.SessionService(Client()).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            }));
            return session.Url;
        }

        public async Task CancelSubscriptionAsync(string subscriptionId)
        {
            try
            {
                await new SubscriptionService(Client()).CancelAsync(subscriptionId);
            }
            catch (StripeException failure) when (failure.StripeError?.Code == "resource_missing" || failure.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
            catch (StripeException failure)
            {
                throw Wrap(failure);
            }
        }

        public StripeEventInfo? ReadWebhook(string payload, string signature)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signature))
            {
                return null;
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret, throwOnApiVersionMismatch: false);
            }
            catch (StripeException)
            {
                return null;
            }

            switch (stripeEvent.Data.Object)
            {
                case Stripe.Checkout.Session session:
                    return new StripeEventInfo(stripeEvent.Id, stripeEvent.Type, session.CustomerId ?? string.Empty, session.SubscriptionId ?? string.Empty,
                        session.ClientReferenceId ?? string.Empty, string.Empty, null, false, null, string.Empty, string.Empty);
                case Subscription subscription:
                    var info = ToInfo(subscription);
                    return new StripeEventInfo(stripeEvent.Id, stripeEvent.Type, info.CustomerId, info.Id, info.UserId, info.Status,
                        info.PeriodEnd, info.CancelAtPeriodEnd, null, string.Empty, string.Empty);
                case Invoice invoice:
                    return new StripeEventInfo(stripeEvent.Id, stripeEvent.Type, invoice.CustomerId ?? string.Empty,
                        invoice.Parent?.SubscriptionDetails?.SubscriptionId ?? string.Empty,
                        invoice.Parent?.SubscriptionDetails?.Metadata is { } meta && meta.TryGetValue("userId", out var owner) ? owner : string.Empty,
                        string.Empty, null, false, (int?)invoice.AmountPaid, invoice.Currency ?? string.Empty, invoice.BillingReason ?? string.Empty);
                default:
                    return new StripeEventInfo(stripeEvent.Id, stripeEvent.Type, string.Empty, string.Empty, string.Empty, string.Empty, null, false, null, string.Empty, string.Empty);
            }
        }

        private static StripeSubscriptionInfo ToInfo(Subscription subscription)
        {
            var end = subscription.Items?.Data?.Select(item => item.CurrentPeriodEnd).DefaultIfEmpty(DateTime.UtcNow).Max() ?? DateTime.UtcNow;
            var userId = subscription.Metadata is not null && subscription.Metadata.TryGetValue("userId", out var id) ? id : string.Empty;
            return new StripeSubscriptionInfo(subscription.Id, subscription.CustomerId ?? string.Empty, userId, subscription.Status ?? string.Empty,
                DateTime.SpecifyKind(end, DateTimeKind.Utc), subscription.CancelAtPeriodEnd);
        }

        private IStripeClient Client()
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                throw new BadRequestException("Payments are not set up yet.");
            }

            return new StripeClient(_settings.SecretKey);
        }

        private static async Task<T> Run<T>(Func<Task<T>> call)
        {
            try
            {
                return await call();
            }
            catch (StripeException failure)
            {
                throw Wrap(failure);
            }
        }

        private static Exception Wrap(StripeException failure)
        {
            return new BadRequestException("The payment service said: " + (failure.StripeError?.Message ?? failure.Message));
        }
    }
}
