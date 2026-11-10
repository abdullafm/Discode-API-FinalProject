namespace Service.Helpers.DTOs.Accounts
{
    public class UpdateSettingsDto
    {
        public string? Theme { get; set; }
        public bool? NotifyAll { get; set; }
        public bool? NotifyDm { get; set; }
        public bool? NotifyFriends { get; set; }
        public bool? NotifySound { get; set; }
    }
}
