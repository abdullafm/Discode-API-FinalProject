using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Helpers.Settings;
using Service.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendVerificationCodeAsync(string to, string code)
        {
            var spaced = string.Join("  ", code.ToCharArray());
            var html = BuildVerificationHtml(code, spaced);
            var plain = $"Your Discode code is {code}. It expires in 10 minutes.";
            return SendBothAsync(to, "Your Discode verification code", plain, html);
        }

        public Task SendAsync(string to, string subject, string body, bool html = false)
        {
            if (html)
                return SendBothAsync(to, subject, string.Empty, body);

            return SendBothAsync(to, subject, body, null);
        }

        private async Task SendBothAsync(string to, string subject, string plain, string? html)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From, "Discode"),
                Subject = subject
            };
            message.To.Add(to);

            if (string.IsNullOrEmpty(html))
            {
                message.Body = plain;
                message.IsBodyHtml = false;
            }
            else
            {
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    string.IsNullOrEmpty(plain) ? "Discode" : plain, Encoding.UTF8, MediaTypeNames.Text.Plain));
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    html, Encoding.UTF8, MediaTypeNames.Text.Html));
            }

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}.", to);
                throw;
            }
        }

        private static string BuildVerificationHtml(string code, string spaced)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>Discode</title>
                </head>
                <body style="margin:0;padding:0;background:#0e0e10;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#0e0e10;padding:32px 12px;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="max-width:480px;width:100%;background:#1e1f22;border-radius:16px;overflow:hidden;border:1px solid #2b2d31;">
                          <tr>
                            <td style="height:6px;background:#5865f2;font-size:0;line-height:0;">&nbsp;</td>
                          </tr>
                          <tr>
                            <td style="padding:36px 36px 12px;text-align:center;">
                              <div style="display:inline-block;width:44px;height:44px;line-height:44px;border-radius:12px;background:#5865f2;color:#fff;font-size:22px;font-weight:800;">D</div>
                              <p style="margin:14px 0 0;color:#f2f3f5;font-size:22px;font-weight:700;letter-spacing:0.04em;">DISCODE</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 36px 0;text-align:center;">
                              <p style="margin:0;color:#f2f3f5;font-size:20px;font-weight:700;">Confirm your email</p>
                              <p style="margin:10px 0 0;color:#b5bac1;font-size:15px;line-height:1.5;">Use this code to finish creating your account.</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:28px 36px 8px;" align="center">
                              <table role="presentation" cellpadding="0" cellspacing="0" style="background:#111214;border:1px solid #3f4147;border-radius:12px;">
                                <tr>
                                  <td style="padding:18px 28px;font-size:36px;font-weight:800;letter-spacing:12px;color:#ffffff;font-family:Consolas,'Courier New',monospace;">
                                    {spaced}
                                  </td>
                                </tr>
                              </table>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 36px 28px;text-align:center;">
                              <p style="margin:0;color:#80848e;font-size:13px;">This code expires in <strong style="color:#dbdee1;">10 minutes</strong>.</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:0 36px 32px;text-align:center;">
                              <p style="margin:0;color:#6d6f78;font-size:12px;line-height:1.5;">If you didn’t create a Discode account, you can ignore this email.</p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
