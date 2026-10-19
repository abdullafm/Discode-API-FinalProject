using Domain.Common;

namespace Domain.Entities
{
    public class Emoji : BaseEntity
    {
        public string Character { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Shortcodes { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int EmojiCategoryId { get; set; }
        public EmojiCategory EmojiCategory { get; set; } = null!;
    }
}
