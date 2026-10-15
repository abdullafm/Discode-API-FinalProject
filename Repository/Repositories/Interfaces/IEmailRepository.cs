namespace Repository.Repositories.Interfaces
{
    public interface IEmailRepository
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task<string> RenderTemplateAsync(string templateUrl, IDictionary<string, string> values);
    }
}
