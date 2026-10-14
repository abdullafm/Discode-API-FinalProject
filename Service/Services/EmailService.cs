using Microsoft.Extensions.Options;
using Service.Helpers.Settings;
using Service.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendVerificationCodeAsync(string to, string code)
        {
            var html =
                $"<p>Your Discode verification code is:</p><h2 style=\"letter-spacing:4px\">{code}</h2>" +
                "<p>It expires in 10 minutes. If you did not request it, ignore this email.</p>";

            await SendAsync(to, "Your Discode verification code", html);
        }

        public Task SendPasswordResetAsync(string to, string resetUrl)
        {
            var link = WebUtility.HtmlEncode(resetUrl);
            var html =
                "<p>We received a request to reset your Discode password.</p>" +
                $"<p><a href=\"{link}\" style=\"display:inline-block;background:#5865F2;color:#ffffff;padding:12px 22px;border-radius:6px;text-decoration:none;font-weight:600;\">Reset password</a></p>" +
                "<p>This link expires in 1 hour. If you did not request it, you can ignore this email.</p>";

            return SendAsync(to, "Reset your Discode password", html);
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From, "Discode"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}
