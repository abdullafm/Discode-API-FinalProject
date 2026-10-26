namespace Service.Helpers.DTOs.ServerChannels
{
    public class ServerChannelDto
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "text";
        public bool IsPrivate { get; set; }
        public int Position { get; set; }
    }
}
