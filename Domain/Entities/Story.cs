using Domain.Common;

namespace Domain.Entities
{
    public class Story : BaseEntity
    {
        public string AuthorId { get; set; } = string.Empty;
        public AppUser Author { get; set; } = null!;
        public string ImageUrl { get; set; } = string.Empty;
        public ICollection<StoryLike> Likes { get; set; } = new List<StoryLike>();
        public ICollection<StoryView> Views { get; set; } = new List<StoryView>();
    }
}
