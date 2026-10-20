namespace Service.Helpers.DTOs.DirectMessages
{
    public class MessageReactionsDto
    {
        public int MessageId { get; set; }
        public List<ReactionDto> Reactions { get; set; } = new();
    }
}
