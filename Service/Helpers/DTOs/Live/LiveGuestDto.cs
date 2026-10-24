namespace Service.Helpers.DTOs.Live
{
    public class LiveGuestDto
    {
        public int StreamId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
