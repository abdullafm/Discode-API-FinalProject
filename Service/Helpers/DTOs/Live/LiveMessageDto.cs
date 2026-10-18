namespace Service.Helpers.DTOs.Live
{
    public class LiveMessageDto
    {
        public int StreamId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
