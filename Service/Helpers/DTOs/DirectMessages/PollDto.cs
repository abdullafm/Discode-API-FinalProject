namespace Service.Helpers.DTOs.DirectMessages
{
    public class PollDto
    {
        public string Question { get; set; } = string.Empty;
        public bool AllowMultiple { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Ended { get; set; }
        public int TotalVoters { get; set; }
        public bool Voted { get; set; }
        public List<PollOptionDto> Options { get; set; } = new();
    }

    public class PollOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public int Votes { get; set; }
        public bool Voted { get; set; }
    }
}
