namespace Service.Helpers.DTOs.ServerMessages
{
    public class ServerMessageUpdateDto
    {
        public int ServerId { get; set; }
        public int ChannelId { get; set; }
        public Dictionary<string, ServerMessageDto> Views { get; set; } = new();
    }
}
