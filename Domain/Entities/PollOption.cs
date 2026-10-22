using Domain.Common;

namespace Domain.Entities
{
    public class PollOption : BaseEntity
    {
        public int PollId { get; set; }
        public Poll Poll { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public int Position { get; set; }
        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
