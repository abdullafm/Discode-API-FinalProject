namespace Service.Helpers.DTOs.Nitro
{
    public class GrantNitroDto
    {
        public string UserName { get; set; } = string.Empty;
        public int Days { get; set; } = 30;
    }

    public class AdjustBoostTimeDto
    {
        public string UserName { get; set; } = string.Empty;
        // above 0: the wait gets longer by this many hours; below 0: shorter
        public int Hours { get; set; }
    }
}
