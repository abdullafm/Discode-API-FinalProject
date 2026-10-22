namespace Service.Helpers.DTOs.Calls
{
    public class CallScreenDto
    {
        public string PeerId { get; set; } = string.Empty;
        public bool PeerIn { get; set; }
        public CallDto PeerView { get; set; } = null!;
    }
}
