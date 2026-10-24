namespace Service.Helpers.DTOs.DirectMessages
{
    public class CreatePollDto
    {
        public string Question { get; set; } = string.Empty;
        public bool AllowMultiple { get; set; }
        public int DurationHours { get; set; } = 24;
        public List<CreatePollOptionDto> Options { get; set; } = new();
    }
}
