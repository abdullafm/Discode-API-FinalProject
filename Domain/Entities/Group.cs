using Domain.Common;

namespace Domain.Entities
{
    public class Group : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public AppUser Owner { get; set; } = null!;
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    }
}
