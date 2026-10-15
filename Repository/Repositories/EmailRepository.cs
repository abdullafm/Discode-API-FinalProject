using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Repository.Settings;
using System.Net;
using System.Net.Mail;

namespace Repository.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly EmailSettings _settings;
        private readonly HttpClient _httpClient;

        public EmailRepository(IOptions<EmailSettings> settings, HttpClient httpClient)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
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

        public async Task<string> RenderTemplateAsync(string templateUrl, IDictionary<string, string> values)
        {
            using var response = await _httpClient.PostAsync(templateUrl, new FormUrlEncodedContent(values));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
