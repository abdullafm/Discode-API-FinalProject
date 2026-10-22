namespace Service.Helpers.DTOs.DirectMessages
{
    public class PollUpdatedDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public PollDto ForUser { get; set; } = null!;
        public PollDto ForRecipient { get; set; } = null!;
    }
}
