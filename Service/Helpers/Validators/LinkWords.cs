using System.Text;

namespace Service.Helpers.Validators
{
    public static class LinkWords
    {
        private static readonly string[] Inside =
        {
            "fuck", "shit", "bitch", "cunt", "whore", "slut", "nigg", "fagg", "asshole", "bastard", "pussy", "dickhead",
            "porn", "nsfw", "hentai", "hitler", "cocksuck", "motherfuck", "bullshit",
            "blyad", "blyat", "pidor", "pidar", "pizd", "mudak", "gandon", "zalupa", "suchka",
            "orospu", "siktir", "sikis", "sikik", "yarrak", "amcik", "amciq", "gotveren", "qehbe", "peyser", "pezevenk", "ananisik"
        };

        private static readonly string[] Whole =
        {
            "rape", "pedo", "nazi", "nude", "ebat", "ebal", "sex", "xxx", "dick", "cock", "ass", "tits", "boob", "anal", "porno", "sik", "sikim", "amk", "got", "pox", "suka", "hui", "huy", "xuy", "nahuy", "nahui", "18plus", "adult"
        };

        private static readonly string[] Innocent = { "scunthorpe", "penistone", "cockburn", "cockermouth", "shiitake" };

        public static bool IsAllowed(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return true;
            }

            var plain = Normalize(link);
            foreach (var word in Innocent)
            {
                plain = plain.Replace(word, string.Empty, StringComparison.Ordinal);
            }

            foreach (var word in Inside)
            {
                if (plain.Contains(word, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var piece in link.ToLowerInvariant().Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var word = Normalize(piece);
                if (Whole.Contains(word))
                {
                    return false;
                }
            }

            return !Whole.Contains(plain);
        }

        private static string Normalize(string value)
        {
            var result = new StringBuilder(value.Length);
            foreach (var raw in value.ToLowerInvariant())
            {
                var c = raw switch
                {
                    '0' => 'o',
                    '1' => 'i',
                    '3' => 'e',
                    '4' => 'a',
                    '5' => 's',
                    '7' => 't',
                    '@' => 'a',
                    '$' => 's',
                    '\u0259' => 'e',
                    '\u0131' => 'i',
                    '\u00f6' => 'o',
                    '\u00fc' => 'u',
                    '\u015f' => 's',
                    '\u00e7' => 'c',
                    '\u011f' => 'g',
                    _ => raw
                };
                if (char.IsLetter(c) || char.IsDigit(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
