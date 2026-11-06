namespace Service.Helpers.DTOs.Groups
{
    public class GroupCallBriefDto
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public bool Video { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
