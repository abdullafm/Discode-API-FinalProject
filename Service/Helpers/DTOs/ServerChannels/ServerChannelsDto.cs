namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerChannelsDto
    {
        public List<ServerChannelDto> Loose { get; set; } = new();
        public List<ServerCategoryDto> Categories { get; set; } = new();
    }
}
