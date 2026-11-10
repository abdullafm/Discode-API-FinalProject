using Microsoft.AspNetCore.SignalR;
using Service.Services.Interfaces;
using System.Collections.Concurrent;

namespace Discode_API_FinalProject.Hubs
{
    public partial class PresenceHub
    {
        private static readonly ConcurrentDictionary<string, long> LastTyping = new();
        private const int TypingEveryMs = 1500;

        public async Task Typing(string kind, string key)
        {
            var now = Environment.TickCount64;
            var slot = UserId + "|" + kind + "|" + key;
            if (LastTyping.TryGetValue(slot, out var before) && now - before < TypingEveryMs)
            {
                return;
            }

            LastTyping[slot] = now;
            if (LastTyping.Count > 5000)
            {
                foreach (var old in LastTyping.Where(x => now - x.Value > 60_000).Select(x => x.Key).ToList())
                {
                    LastTyping.TryRemove(old, out _);
                }
            }

            using var scope = _scopes.CreateScope();
            var typing = await scope.ServiceProvider.GetRequiredService<ITypingService>().ResolveAsync(UserId, kind, key);
            if (typing is null || typing.AudienceIds.Count == 0)
            {
                return;
            }

            await Clients.Users(typing.AudienceIds).SendAsync("UserTyping", new
            {
                kind = typing.Kind,
                key = typing.Key,
                userId = UserId,
                userName = typing.UserName,
                displayName = typing.DisplayName
            });
        }
    }
}
