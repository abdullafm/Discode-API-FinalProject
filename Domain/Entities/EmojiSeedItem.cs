using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class EmojiSeedItem
    {
        [JsonPropertyName("c")]
        public string Character { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("s")]
        public string Shortcodes { get; set; } = string.Empty;
    }
}
