using Domain.Common;

namespace Domain.Entities
{
    public class Comment : BaseEntity
    {
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string AuthorId { get; set; } = string.Empty;
        public AppUser Author { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    }
}
