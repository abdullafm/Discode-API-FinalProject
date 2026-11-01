namespace Service.Helpers.DTOs.Admin
{
    public class AdminListFilterDto
    {
        public string? Search { get; set; }
        public string? Role { get; set; }
        public bool BannedOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
