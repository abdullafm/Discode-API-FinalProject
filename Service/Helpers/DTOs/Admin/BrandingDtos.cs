namespace Service.Helpers.DTOs.Admin
{
    public class BrandingDto
    {
        public string LogoUrl { get; set; } = string.Empty;
        public string FaviconUrl { get; set; } = string.Empty;
    }

    public class SetBrandingDto
    {
        public string Url { get; set; } = string.Empty;
    }
}
