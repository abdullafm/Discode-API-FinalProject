using Domain.Common;

namespace Domain.Entities
{
    public class ConversationPin : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string Key { get; set; } = string.Empty;
    }
}
