namespace Service.Helpers
{
    public static class LetterAvatar
    {
        public const string Prefix = "/avatar/letter/";

        public static string For(string? userName)
        {
            var first = (userName ?? string.Empty).Trim().FirstOrDefault();
            return Prefix + (char.IsAsciiLetterOrDigit(first) ? char.ToUpperInvariant(first).ToString() : "_");
        }

        public static bool IsLetter(string? url)
        {
            return !string.IsNullOrEmpty(url) && url.StartsWith(Prefix, StringComparison.Ordinal);
        }
    }
}
