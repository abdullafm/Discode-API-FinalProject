namespace Service.Helpers.DTOs.Servers
{
    public class UpdateServerProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BannerColor { get; set; } = "ink";
        public bool IsPrivate { get; set; }
        public string? IconUrl { get; set; }
    }
}
