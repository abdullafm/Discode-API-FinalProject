using Domain.Common;

namespace Domain.Entities
{
    public class StoryView : BaseEntity
    {
        public int StoryId { get; set; }
        public Story Story { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
