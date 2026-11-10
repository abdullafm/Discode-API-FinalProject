namespace Service.Helpers
{
    public static class SlowModeRules
    {
        public static readonly int[] Allowed = { 0, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600, 7200, 21600 };

        public static bool IsAllowed(int seconds)
        {
            return Array.IndexOf(Allowed, seconds) >= 0;
        }

        public static string Describe(int seconds)
        {
            if (seconds <= 0)
            {
                return "off";
            }

            if (seconds < 60)
            {
                return seconds + " seconds";
            }

            if (seconds < 3600)
            {
                return seconds / 60 + (seconds / 60 == 1 ? " minute" : " minutes");
            }

            return seconds / 3600 + (seconds / 3600 == 1 ? " hour" : " hours");
        }
    }
}
