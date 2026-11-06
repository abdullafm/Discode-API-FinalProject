namespace Service.Helpers.DTOs.Admin
{
    public class AdminContentItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? PostId { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
        public int Views { get; set; }
        public int OpenReports { get; set; }
    }

    public class AdminContentListDto
    {
        public string Type { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PostCount { get; set; }
        public int CommentCount { get; set; }
        public int StoryCount { get; set; }
        public List<AdminContentItemDto> Items { get; set; } = new();
    }
}
