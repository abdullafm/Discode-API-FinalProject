using Domain.Common;

namespace Domain.Entities
{
    public class GroupMessageReaction : BaseEntity
    {
        public int GroupMessageId { get; set; }
        public GroupMessage GroupMessage { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string Emoji { get; set; } = string.Empty;
    }
}
