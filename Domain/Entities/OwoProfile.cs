using Domain.Common;

namespace Domain.Entities
{
    public class OwoProfile : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public long Cowoncy { get; set; }
        public int Luck { get; set; }
        public int DailyStreak { get; set; }
        public int Hunts { get; set; }
        public int BattlesWon { get; set; }
        public int BattlesLost { get; set; }
        public DateTime? LastDailyAt { get; set; }
        public DateTime? LastHuntAt { get; set; }
        public DateTime? LastBattleAt { get; set; }
        public DateTime? LastPrayAt { get; set; }
        public DateTime? LastOwoAt { get; set; }
        public DateTime? LastGambleAt { get; set; }
    }
}
