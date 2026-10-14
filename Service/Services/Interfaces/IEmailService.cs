namespace Service.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task SendVerificationCodeAsync(string to, string code);
        Task SendPasswordResetAsync(string to, string resetUrl);
    }
}
