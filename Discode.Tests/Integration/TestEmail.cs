using Repository.Repositories.Interfaces;

namespace Discode.Tests.Integration
{
    public class TestEmail : IEmailRepository
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string htmlBody)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }

        public Task<string> RenderTemplateAsync(string templateUrl, IDictionary<string, string> values)
        {
            return Task.FromResult(string.Join(" ", values.Select(pair => pair.Value)));
        }
    }
}
