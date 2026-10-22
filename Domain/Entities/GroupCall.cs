using Domain.Common;

namespace Domain.Entities
{
    public class GroupCall : BaseEntity
    {
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
        public string StarterId { get; set; } = string.Empty;
        public AppUser Starter { get; set; } = null!;
        public bool Video { get; set; }
        public bool Active { get; set; } = true;
        public DateTime? EndedAt { get; set; }
        public int GroupMessageId { get; set; }
        public GroupMessage GroupMessage { get; set; } = null!;
        public ICollection<GroupCallParticipant> Participants { get; set; } = new List<GroupCallParticipant>();
    }
}
