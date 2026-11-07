namespace Service.Helpers.DTOs.Nitro
{
    public class BoostChangeDto
    {
        public BoostInfoDto Info { get; set; } = new();
        public NitroChangeDto Change { get; set; } = new();
    }
}
