namespace Service.Helpers.DTOs.Groups
{
    public class SendGroupMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public int? ReplyToMessageId { get; set; }
        public List<Service.Helpers.DTOs.DirectMessages.MessageAttachmentDto> Attachments { get; set; } = new();
    }
}
