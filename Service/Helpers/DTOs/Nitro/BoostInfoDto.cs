namespace Service.Helpers.DTOs.Nitro
{
    public class BoostInfoDto
    {
        public int ServerId { get; set; }
        public int Boosts { get; set; }
        public int Required { get; set; }
        public bool PerksUnlocked { get; set; }
        public int MyBoosts { get; set; }
        public int MyBoostsLeft { get; set; }
        public bool CanBoost { get; set; }
        public DateTime? CooldownUntil { get; set; }
        public List<BoosterDto> Boosters { get; set; } = new();
    }

    public class BoosterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
