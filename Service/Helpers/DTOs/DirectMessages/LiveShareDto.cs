namespace Service.Helpers.DTOs.DirectMessages
{
    public class LiveShareDto
    {
        public int StreamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
