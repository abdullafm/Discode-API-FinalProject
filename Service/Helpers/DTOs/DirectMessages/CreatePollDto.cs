namespace Service.Helpers.DTOs.DirectMessages
{
    public class CreatePollDto
    {
        public string Question { get; set; } = string.Empty;
        public bool AllowMultiple { get; set; }
        public int DurationHours { get; set; } = 24;
        public List<CreatePollOptionDto> Options { get; set; } = new();
    }

    public class CreatePollOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
    }
}
