using Domain.Common;

namespace Domain.Entities
{
    public class PostLike : BaseEntity
    {
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
