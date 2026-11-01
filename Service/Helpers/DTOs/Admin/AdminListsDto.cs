namespace Service.Helpers.DTOs.Admin
{
    public class AdminUserRowDto
    {
        public string Id { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public int ServersJoined { get; set; }
        public int BansReceived { get; set; }
    }

    public class AdminUserListDto
    {
        public List<AdminUserRowDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminServerRowDto
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string OwnerUserName { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Members { get; set; }
        public int Bans { get; set; }
    }

    public class AdminServerListDto
    {
        public List<AdminServerRowDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
