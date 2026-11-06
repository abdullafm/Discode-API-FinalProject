using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IDirectMessageRepository
    {
        Task<IEnumerable<Message>> GetChatMessagesAsync(string userId, string otherId, int count);
        Task<MessageForward> ForwardAsync(MessageForward forward);
    }
}
