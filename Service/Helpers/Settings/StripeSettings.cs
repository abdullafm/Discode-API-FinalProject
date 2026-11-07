namespace Service.Helpers.Settings
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        // the signing secret of the webhook (whsec_...): without it a webhook is refused
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
