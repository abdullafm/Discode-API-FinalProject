namespace Service.Helpers
{
    public record OwoAnimalInfo(string Key, string Emoji, string Name, int Rarity);

    public static class OwoRules
    {
        public static readonly string[] RarityNames = { "common", "uncommon", "rare", "epic", "mythical", "legendary" };
        public static readonly string[] RarityMarks = { "🌿", "🍀", "🌸", "💎", "🔮", "👑" };
        public static readonly int[] SellPrice = { 1, 5, 20, 100, 500, 2500 };
        public static readonly int[] Power = { 1, 3, 9, 25, 80, 250 };
        private static readonly double[] Chance = { 0.62, 0.24, 0.09, 0.035, 0.012, 0.003 };

        public static readonly OwoAnimalInfo[] Animals =
        {
            new("bug", "🐛", "Bug", 0), new("ant", "🐜", "Ant", 0), new("ladybug", "🐞", "Ladybug", 0), new("snail", "🐌", "Snail", 0), new("mouse", "🐁", "Mouse", 0),
            new("bee", "🐝", "Bee", 1), new("rabbit", "🐇", "Rabbit", 1), new("chicken", "🐓", "Chicken", 1), new("pig", "🐖", "Pig", 1), new("cat", "🐈", "Cat", 1),
            new("fox", "🦊", "Fox", 2), new("wolf", "🐺", "Wolf", 2), new("deer", "🦌", "Deer", 2), new("owl", "🦉", "Owl", 2), new("boar", "🐗", "Boar", 2),
            new("lion", "🦁", "Lion", 3), new("tiger", "🐯", "Tiger", 3), new("bear", "🐻", "Bear", 3), new("elephant", "🐘", "Elephant", 3),
            new("unicorn", "🦄", "Unicorn", 4), new("dragon", "🐲", "Dragon", 4), new("eagle", "🦅", "Eagle", 4),
            new("trex", "🦖", "T-Rex", 5), new("kraken", "🦑", "Kraken", 5)
        };

        public static OwoAnimalInfo? Find(string key)
        {
            return Animals.FirstOrDefault(a => a.Key == key);
        }

        public static OwoAnimalInfo Roll(Random random, int luck)
        {
            var roll = random.NextDouble() - Math.Min(luck, 100) * 0.0008;
            var rarity = 0;
            var total = 0.0;
            for (var i = 0; i < Chance.Length; i++)
            {
                total += Chance[i];
                if (roll < total)
                {
                    rarity = i;
                    break;
                }

                rarity = Chance.Length - 1;
            }

            var pool = Animals.Where(a => a.Rarity == rarity).ToArray();
            return pool[random.Next(pool.Length)];
        }

        public static string Wait(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return (int)span.TotalHours + "H " + span.Minutes + "M";
            }

            if (span.TotalMinutes >= 1)
            {
                return span.Minutes + "M " + span.Seconds + "S";
            }

            return Math.Max(1, (int)Math.Ceiling(span.TotalSeconds)) + "S";
        }

        public static string Money(long value)
        {
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
