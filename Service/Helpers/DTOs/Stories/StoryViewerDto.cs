namespace Service.Helpers.DTOs.Stories
{
    public class StoryViewerDto
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool Liked { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}
