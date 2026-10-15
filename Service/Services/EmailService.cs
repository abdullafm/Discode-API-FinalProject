using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepository;
        private readonly AppSettings _appSettings;

        public EmailService(IEmailRepository emailRepository, IOptions<AppSettings> appSettings)
        {
            _emailRepository = emailRepository;
            _appSettings = appSettings.Value;
        }

        public async Task SendVerificationCodeAsync(string to, string code)
        {
            var html = await RenderAsync("VerificationCode", new Dictionary<string, string>
            {
                ["code"] = code
            });

            await SendAsync(to, "Your Discode verification code", html);
        }

        public async Task SendPasswordResetAsync(string to, string resetUrl)
        {
            var html = await RenderAsync("PasswordReset", new Dictionary<string, string>
            {
                ["resetUrl"] = resetUrl
            });

            await SendAsync(to, "Reset your Discode password", html);
        }

        public Task SendAsync(string to, string subject, string htmlBody)
        {
            return _emailRepository.SendAsync(to, subject, htmlBody);
        }

        private Task<string> RenderAsync(string template, Dictionary<string, string> values)
        {
            var url = $"{_appSettings.ClientUrl.TrimEnd('/')}/EmailTemplates/{template}";
            return _emailRepository.RenderTemplateAsync(url, values);
        }
    }
}
