using Domain.Common;

namespace Domain.Entities
{
    public class EmojiCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public ICollection<Emoji> Emojis { get; set; } = new List<Emoji>();
    }
}
