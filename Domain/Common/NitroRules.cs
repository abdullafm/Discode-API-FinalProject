namespace Domain.Common
{
    public static class NitroRules
    {
        public const int FreeMessageLength = 2000;
        public const int NitroMessageLength = 50000;
        public const long FreeFileBytes = 5L * 1024 * 1024;
        public const long NitroFileBytes = 2L * 1024 * 1024 * 1024;
        public const int BoostsPerNitro = 2;
        public const int BoostsForPerks = 14;
        public const int BoostCooldownHours = 24;
        public const int MonthlyPriceCents = 299;
        public const int GiftPriceCents = 999;
        public const int GiftDays = 30;
        public const string Currency = "usd";

        public static bool IsAnimatedImage(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var path = url.Trim();
            var cut = path.IndexOfAny(new[] { '?', '#' });
            if (cut >= 0)
            {
                path = path.Substring(0, cut);
            }

            return path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        }
    }
}
