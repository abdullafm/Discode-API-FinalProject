namespace Service.Helpers.DTOs.Groups
{
    public class GroupMessageDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string SenderUserName { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public Service.Helpers.DTOs.DirectMessages.ReplyDto? ReplyTo { get; set; }
        public bool IsPoll { get; set; }
        public Service.Helpers.DTOs.DirectMessages.PollDto? Poll { get; set; }
        public List<Service.Helpers.DTOs.DirectMessages.MessageAttachmentDto> Attachments { get; set; } = new();
        public List<Service.Helpers.DTOs.DirectMessages.ReactionDto> Reactions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
