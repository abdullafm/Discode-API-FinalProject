namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public int Position { get; set; }
        public List<ServerChannelDto> Channels { get; set; } = new();
    }
}
