namespace Service.Helpers.DTOs.Discover
{
    public class DiscoverFilterDto
    {
        public string? Search { get; set; }
        public int Take { get; set; } = 60;
    }
}
