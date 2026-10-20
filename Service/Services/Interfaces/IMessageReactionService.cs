namespace Service.Services.Interfaces
{
    public interface IMessageReactionService
    {
        Task ToggleAsync(int messageId, string userId, string emoji);
    }
}
