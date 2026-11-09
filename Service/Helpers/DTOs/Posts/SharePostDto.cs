namespace Service.Helpers.DTOs.Posts
{
    // Send a post to friends, the way Instagram does: each friend gets it in their direct messages
    public class SharePostDto
    {
        public List<string> UserNames { get; set; } = new();
        public string Note { get; set; } = string.Empty;
    }
}
