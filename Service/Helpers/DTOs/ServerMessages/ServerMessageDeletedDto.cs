namespace Service.Helpers.DTOs.ServerMessages
{
    public class ServerMessageDeletedDto
    {
        public int ServerId { get; set; }
        public int ChannelId { get; set; }
        public int MessageId { get; set; }
        public List<string> ViewerIds { get; set; } = new();
    }
}
