namespace Service.Helpers.DTOs.Posts
{
    public class CreateCommentDto
    {
        public string Text { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
    }
}
