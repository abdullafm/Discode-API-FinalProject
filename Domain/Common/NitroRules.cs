namespace Domain.Common
{
    // The numbers that make Nitro different from a free account
    public static class NitroRules
    {
        public const int FreeMessageLength = 2000;
        // "unlimited" for a person, but a message still has to fit in the database
        public const int NitroMessageLength = 50000;
        public const long FreeFileBytes = 5L * 1024 * 1024;
        public const long NitroFileBytes = 2L * 1024 * 1024 * 1024;
        public const int BoostsPerNitro = 2;
        public const int BoostsForPerks = 14;
        public const int MonthlyPriceCents = 299;
        public const string Currency = "usd";

        // GIF avatars and icons are the animated ones
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
