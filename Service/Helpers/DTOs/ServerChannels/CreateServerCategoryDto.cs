namespace Service.Helpers.DTOs.ServerChannels
{
    public class CreateServerCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
    }
}
