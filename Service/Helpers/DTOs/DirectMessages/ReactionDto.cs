namespace Service.Helpers.DTOs.DirectMessages
{
    public class ReactionDto
    {
        public string Emoji { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool Reacted { get; set; }
    }
}
