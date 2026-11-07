namespace Service.Helpers.DTOs.Nitro
{
    public class CreateCheckoutDto
    {
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
