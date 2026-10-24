namespace Service.Helpers.DTOs.Live
{
    public class LiveGuestRequestDto
    {
        public string HostConnectionId { get; set; } = string.Empty;
        public LiveGuestDto Guest { get; set; } = null!;
    }
}
