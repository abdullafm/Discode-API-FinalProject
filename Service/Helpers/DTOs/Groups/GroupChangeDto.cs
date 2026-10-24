namespace Service.Helpers.DTOs.Groups
{
    public class GroupChangeDto
    {
        public int GroupId { get; set; }
        public GroupDto? Group { get; set; }
        public List<string> UserIds { get; set; } = new();
        public GroupMessageDto? Message { get; set; }
    }
}
