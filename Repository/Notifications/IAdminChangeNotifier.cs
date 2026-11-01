namespace Repository.Notifications
{
    public interface IAdminChangeNotifier
    {
        Task NotifyAsync(IReadOnlyCollection<string> topics, IReadOnlyCollection<int> serverIds);
    }
}
