namespace Service.Helpers.DTOs.ServerPrune
{
    public class ServerPruneChangeDto
    {
        public int Count { get; set; }
        public List<string> RemovedUserIds { get; set; } = new();
        public List<string> RemainingUserIds { get; set; } = new();
    }
}
