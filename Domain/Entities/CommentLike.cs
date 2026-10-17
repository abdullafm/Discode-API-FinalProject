using Domain.Common;

namespace Domain.Entities
{
    public class CommentLike : BaseEntity
    {
        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
