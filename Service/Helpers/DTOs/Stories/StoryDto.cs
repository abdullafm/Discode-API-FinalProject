namespace Service.Helpers.DTOs.Stories
{
    public class StoryDto
    {
        public int Id { get; set; }
        public string AuthorUserName { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByMe { get; set; }
    }
}
