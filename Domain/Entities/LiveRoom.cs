namespace Domain.Entities
{
    public class LiveRoom
    {
        public string HostConnectionId { get; set; } = string.Empty;
        public HashSet<string> Viewers { get; } = new();
    }
}
