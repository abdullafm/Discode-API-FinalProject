using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class EmojiSeedCategory
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("emojis")]
        public List<EmojiSeedItem> Emojis { get; set; } = new();
    }
}
