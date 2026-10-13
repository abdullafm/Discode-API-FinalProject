namespace Service.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, bool html = false);
        Task SendVerificationCodeAsync(string to, string code);
    }
}
