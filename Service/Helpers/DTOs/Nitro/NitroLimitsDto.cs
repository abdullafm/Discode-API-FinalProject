namespace Service.Helpers.DTOs.Nitro
{
    public class NitroLimitsDto
    {
        public bool HasNitro { get; set; }
        public int MaxMessageLength { get; set; }
        public long MaxFileBytes { get; set; }
    }
}
