namespace Service.Helpers.DTOs.Groups
{
    public class GroupCallRingDto
    {
        public string UserId { get; set; } = string.Empty;
        public GroupCallDto Call { get; set; } = new();
    }
}
