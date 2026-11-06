namespace Service.Helpers.DTOs.ServerChannels
{
    public class CreateServerChannelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "text";
        public int? CategoryId { get; set; }
        public bool IsPrivate { get; set; }
        public int UserLimit { get; set; }
    }
}
