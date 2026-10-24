using Domain.Common;

namespace Domain.Entities
{
    public class UserIgnore : BaseEntity
    {
        public string IgnorerId { get; set; } = string.Empty;
        public AppUser Ignorer { get; set; } = null!;
        public string IgnoredId { get; set; } = string.Empty;
        public AppUser Ignored { get; set; } = null!;
    }
}
