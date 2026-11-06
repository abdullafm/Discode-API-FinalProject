namespace Service.Helpers.DTOs.Servers
{
    public class UpdateServerPreferencesDto
    {
        public int? MuteMinutes { get; set; }
        public string? NotificationLevel { get; set; }
        public bool? HideMutedChannels { get; set; }
        public bool? ShowAllChannels { get; set; }
    }
}
