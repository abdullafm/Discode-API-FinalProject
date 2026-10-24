using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Service.Services
{
    public class MentionService : IMentionService
    {
        private static readonly Regex Pattern = new(@"(?<![\w@])@([A-Za-z0-9_.\-]{2,})", RegexOptions.Compiled);

        private readonly IAccountRepository _accounts;
        private readonly IFriendRequestRepository _friendRequests;
        private readonly INotificationService _notifications;

        public MentionService(IAccountRepository accounts, IFriendRequestRepository friendRequests, INotificationService notifications)
        {
            _accounts = accounts;
            _friendRequests = friendRequests;
            _notifications = notifications;
        }

        public async Task NotifyAsync(NotificationType type, string actorId, string text, int? postId = null, int? commentId = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var done = new HashSet<string>();
            foreach (Match match in Pattern.Matches(text))
            {
                var token = match.Groups[1].Value;
                while (token.Length >= 2)
                {
                    var user = await _accounts.GetByUserNameAsync(token);
                    if (user is not null)
                    {
                        if (user.Id != actorId && done.Add(user.Id))
                        {
                            var friendship = await _friendRequests.GetBetweenAsync(actorId, user.Id);
                            if (friendship is not null && friendship.Status == FriendRequestStatus.Accepted)
                            {
                                await _notifications.AddAsync(type, actorId, user.Id, postId: postId, commentId: commentId, snippet: text);
                            }
                        }

                        break;
                    }

                    token = token.Substring(0, token.Length - 1);
                }
            }
        }
    }
}
