using Service.Helpers.DTOs.DirectMessages;

namespace Service.Helpers.DTOs.ServerMessages
{
    public class SendServerMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public int? ReplyToMessageId { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
    }
}
