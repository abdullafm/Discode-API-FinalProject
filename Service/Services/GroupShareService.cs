using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Groups;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class GroupShareService : IGroupShareService
    {
        public const int MaxGroups = 10;

        private readonly IGroupService _groups;
        private readonly IDirectMessageService _directMessages;
        private readonly IAccountRepository _accounts;

        public GroupShareService(IGroupService groups, IDirectMessageService directMessages, IAccountRepository accounts)
        {
            _groups = groups;
            _directMessages = directMessages;
            _accounts = accounts;
        }

        private static List<int> Clean(IEnumerable<int>? ids)
        {
            var list = (ids ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
            if (list.Count > MaxGroups)
            {
                throw new BadRequestException("Pick at most " + MaxGroups + " groups.");
            }

            return list;
        }

        public async Task<List<GroupChangeDto>> SendTextAsync(string userId, IEnumerable<int>? groupIds, string text)
        {
            var changes = new List<GroupChangeDto>();
            foreach (var id in Clean(groupIds))
            {
                changes.Add(await _groups.SendMessageAsync(userId, id, new SendGroupMessageDto { Text = text }));
            }

            return changes;
        }

        public async Task<List<GroupChangeDto>> ForwardFromDmAsync(string userId, int messageId, IEnumerable<int>? groupIds, string note)
        {
            var ids = Clean(groupIds);
            if (ids.Count == 0)
            {
                return new List<GroupChangeDto>();
            }

            return await ForwardAsync(userId, await _directMessages.GetForwardSourceAsync(userId, messageId), ids, note);
        }

        public async Task<List<GroupChangeDto>> ForwardFromGroupAsync(string userId, int groupId, int messageId, IEnumerable<int>? groupIds, string note)
        {
            var ids = Clean(groupIds);
            if (ids.Count == 0)
            {
                return new List<GroupChangeDto>();
            }

            return await ForwardAsync(userId, await _groups.GetForwardSourceAsync(userId, groupId, messageId), ids, note);
        }

        private async Task<List<GroupChangeDto>> ForwardAsync(string userId, ForwardSourceDto source, List<int> ids, string note)
        {
            var original = await _accounts.GetByIdAsync(source.OriginalSenderId);
            var who = original is null ? "someone" : string.IsNullOrWhiteSpace(original.DisplayName) ? original.UserName ?? "someone" : original.DisplayName;
            var text = "↪ Forwarded from **" + who + "**" + (source.Text.Length > 0 ? "\n" + source.Text : string.Empty);
            note = (note ?? string.Empty).Trim();
            var changes = new List<GroupChangeDto>();
            foreach (var id in ids)
            {
                changes.Add(await _groups.SendMessageAsync(userId, id, new SendGroupMessageDto { Text = text, Attachments = source.Attachments }));
                if (note.Length > 0)
                {
                    changes.Add(await _groups.SendMessageAsync(userId, id, new SendGroupMessageDto { Text = note }));
                }
            }

            return changes;
        }
    }
}
