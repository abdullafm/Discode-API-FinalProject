namespace Service.Helpers.DTOs.ServerBans
{
    public class ServerBanFilterDto
    {
        public string? Search { get; set; }
        public int? BeforeId { get; set; }
        public int Take { get; set; } = 50;
    }
}
