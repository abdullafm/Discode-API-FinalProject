using Domain.Common;

namespace Domain.Entities
{
    public class PollVote : BaseEntity
    {
        public int OptionId { get; set; }
        public PollOption Option { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
    }
}
