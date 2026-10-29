namespace Service.Helpers.DTOs.ServerMessages
{
    public class ServerMessageChangeDto
    {
        public ServerMessageDto Message { get; set; } = new();
        public List<string> ViewerIds { get; set; } = new();
        public Dictionary<string, int> MentionCounts { get; set; } = new();
    }
}
