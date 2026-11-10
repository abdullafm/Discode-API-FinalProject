using Domain.Common;

namespace Domain.Entities
{
    public class OwoAnimal : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string AnimalKey { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
