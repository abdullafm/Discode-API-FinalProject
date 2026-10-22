namespace Service.Helpers.DTOs.Posts
{
    public class CreatePostDto
    {
        public string Text { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Visibility { get; set; } = "public";
    }
}
