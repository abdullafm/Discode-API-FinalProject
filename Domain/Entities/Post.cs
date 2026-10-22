using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Post : BaseEntity
    {
        public string AuthorId { get; set; } = string.Empty;
        public AppUser Author { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
