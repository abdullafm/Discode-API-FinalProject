namespace Service.Helpers.DTOs.Live
{
    public class LiveDisconnectDto
    {
        public IEnumerable<int> EndedStreamIds { get; set; } = Enumerable.Empty<int>();
        public Dictionary<int, int> ViewerCounts { get; set; } = new();
    }
}
