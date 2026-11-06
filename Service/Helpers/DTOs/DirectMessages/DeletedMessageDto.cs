namespace Service.Helpers.DTOs.DirectMessages
{
    public class DeletedMessageDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
    }
}
