namespace Service.Helpers.DTOs.Posts
{
    public class PostDto
    {
        public int Id { get; set; }
        public string AuthorUserName { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Visibility { get; set; } = "public";
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByMe { get; set; }
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
    }
}
