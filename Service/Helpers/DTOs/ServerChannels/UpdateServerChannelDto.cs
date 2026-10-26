namespace Service.Helpers.DTOs.ServerChannels
{
    public class UpdateServerChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public bool IsReadOnly { get; set; }
    }
}
