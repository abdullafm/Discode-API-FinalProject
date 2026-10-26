namespace Service.Helpers.DTOs.ServerChannels
{
    public class UpdateServerCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public List<ServerOverwriteInputDto>? Overwrites { get; set; }
    }
}
