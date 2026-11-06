namespace Service.Helpers.DTOs.Groups
{
    public class GroupMessageDeletedDto
    {
        public int GroupId { get; set; }
        public int MessageId { get; set; }
        public List<string> UserIds { get; set; } = new();
    }
}
