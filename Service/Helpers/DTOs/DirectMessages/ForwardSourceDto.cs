namespace Service.Helpers.DTOs.DirectMessages
{
    public class ForwardSourceDto
    {
        public string OriginalSenderId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
    }
}
