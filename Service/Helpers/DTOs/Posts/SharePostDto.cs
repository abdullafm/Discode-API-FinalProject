namespace Service.Helpers.DTOs.Posts
{
    public class SharePostDto
    {
        public List<string> UserNames { get; set; } = new();
        public string Note { get; set; } = string.Empty;
    }
}
