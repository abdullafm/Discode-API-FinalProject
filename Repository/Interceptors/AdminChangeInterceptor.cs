using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Repository.Notifications;

namespace Repository.Interceptors
{
    // Tells the admin panel which areas changed after a successful save, so it can refresh live
    public class AdminChangeInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _services;
        private readonly HashSet<string> _topics = new();
        private readonly HashSet<int> _serverIds = new();

        public AdminChangeInterceptor(IServiceProvider services)
        {
            _services = services;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Collect(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Collect(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            FlushAsync().GetAwaiter().GetResult();
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await FlushAsync();
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            Clear();
            base.SaveChangesFailed(eventData);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Clear();
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private void Collect(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries())
            {
                var added = entry.State == EntityState.Added;
                var deleted = entry.State == EntityState.Deleted;
                var modified = entry.State == EntityState.Modified;
                if (!added && !deleted && !modified)
                {
                    continue;
                }

                switch (entry.Entity)
                {
                    case AppUser:
                        if (added || deleted || ProfileChanged(entry))
                        {
                            _topics.Add("users");
                        }
                        break;
                    case IdentityUserRole<string>:
                        _topics.Add("users");
                        break;
                    case Server server:
                        _topics.Add("servers");
                        _serverIds.Add(server.Id);
                        break;
                    case ServerMember member when added || deleted:
                        _topics.Add("servers");
                        _topics.Add("users");
                        _serverIds.Add(member.ServerId);
                        break;
                    case ServerBan ban when added || deleted:
                        _topics.Add("servers");
                        _topics.Add("users");
                        _serverIds.Add(ban.ServerId);
                        break;
                    case ServerChannel channel when added || deleted:
                        _topics.Add("server-detail");
                        _serverIds.Add(channel.ServerId);
                        break;
                    case ServerRole role when added || deleted:
                        _topics.Add("server-detail");
                        _serverIds.Add(role.ServerId);
                        break;
                    case ServerMessage message when added || deleted:
                        _topics.Add("server-detail");
                        _serverIds.Add(message.ServerId);
                        break;
                    case UserWarning:
                        _topics.Add("users");
                        break;
                    case AdminAction:
                        _topics.Add("activity");
                        break;
                    case Report:
                        _topics.Add("reports");
                        break;
                    case LiveStream:
                        _topics.Add("live");
                        break;
                    case Post when added || deleted:
                    case Comment when added || deleted:
                        _topics.Add("content");
                        break;
                }
            }
        }

        private static bool ProfileChanged(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            foreach (var name in new[] { "DisplayName", "UserName", "Email", "AvatarUrl", "Bio", "Pronouns", "CustomStatus", "IsPublic", "IsBanned", "TimeoutUntil", "IsDeleted", "LiveBanned" })
            {
                if (entry.Property(name).IsModified)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task FlushAsync()
        {
            if (_topics.Count == 0)
            {
                return;
            }

            var topics = _topics.ToList();
            var serverIds = _serverIds.Where(id => id > 0).ToList();
            Clear();

            try
            {
                var notifier = _services.GetService<IAdminChangeNotifier>();
                if (notifier is not null)
                {
                    await notifier.NotifyAsync(topics, serverIds);
                }
            }
            catch (Exception)
            {
                // a failed live notification must never break the save that already succeeded
            }
        }

        private void Clear()
        {
            _topics.Clear();
            _serverIds.Clear();
        }
    }
}
