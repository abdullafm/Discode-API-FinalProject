namespace Service.Helpers.DTOs.Nitro
{
    // The "Server Boosts" page of the settings: my boosts, which are free, and every server I could boost
    public class MyBoostsDto
    {
        public bool HasNitro { get; set; }
        public int Slots { get; set; }
        public int Used { get; set; }
        public int Free { get; set; }
        public int Required { get; set; }
        public int CooldownHours { get; set; }
        // when each taken-off boost is free again
        public List<DateTime> Cooldowns { get; set; } = new();
        public List<MyBoostDto> Boosts { get; set; } = new();
        public List<BoostableServerDto> Servers { get; set; } = new();
    }

    public class MyBoostDto
    {
        public int ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public DateTime BoostedAt { get; set; }
        // from then on this boost can be moved to another server
        public DateTime MovableAt { get; set; }
        public bool CanMove { get; set; }
    }

    public class BoostableServerDto
    {
        public int ServerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int Boosts { get; set; }
        public bool PerksUnlocked { get; set; }
        public int MyBoosts { get; set; }
    }
}
