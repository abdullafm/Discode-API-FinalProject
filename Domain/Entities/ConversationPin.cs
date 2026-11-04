using Domain.Common;

namespace Domain.Entities
{
    // A person pinned to the top of someone's direct message list
    public class ConversationPin : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        // the user name of the pinned person, lower case
        public string Key { get; set; } = string.Empty;
    }
}
