namespace Service.Helpers.DTOs.Groups
{
    public class GroupCallChangeDto
    {
        public GroupCallDto Call { get; set; } = new();
        public List<string> MemberIds { get; set; } = new();
        public bool Started { get; set; }
        public bool Ended { get; set; }
        public GroupMessageDto? Message { get; set; }
        public List<GroupCallChangeDto> Left { get; set; } = new();
    }
}
