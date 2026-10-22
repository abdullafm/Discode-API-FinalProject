namespace Service.Helpers.DTOs.Groups
{
    public class SendGroupMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public List<Service.Helpers.DTOs.DirectMessages.MessageAttachmentDto> Attachments { get; set; } = new();
    }

    public class UpdateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
    }

    public class AddGroupMembersDto
    {
        public List<string> UserNames { get; set; } = new();
    }

    public class GroupMessageUpdateDto
    {
        public int GroupId { get; set; }
        public Dictionary<string, GroupMessageDto> Views { get; set; } = new();
    }

    public class GroupChangeDto
    {
        public int GroupId { get; set; }
        public GroupDto? Group { get; set; }
        public List<string> UserIds { get; set; } = new();
        public GroupMessageDto? Message { get; set; }
    }
}
