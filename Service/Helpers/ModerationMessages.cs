namespace Service.Helpers
{
    public static class ModerationMessages
    {
        public static bool IsBanActive(bool isBanned, DateTime? bannedUntil, DateTime now)
        {
            return isBanned && (bannedUntil is null || bannedUntil > now);
        }

        public static string Banned(DateTime? until, string reason)
        {
            var when = until is null ? "permanently" : "until " + until.Value.ToString("yyyy-MM-dd HH:mm") + " UTC";
            var why = string.IsNullOrWhiteSpace(reason) ? string.Empty : " Reason: " + reason;
            return "Your account is banned " + when + "." + why;
        }

        public static string TimedOut(DateTime until, string reason)
        {
            var why = string.IsNullOrWhiteSpace(reason) ? string.Empty : " Reason: " + reason;
            return "You are timed out until " + until.ToString("yyyy-MM-dd HH:mm") + " UTC and cannot post or send messages." + why;
        }
    }
}
