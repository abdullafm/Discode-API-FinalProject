using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Live;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    // The host and the moderators the host picked look after the chat of the host's streams
    public class LiveModerationService : ILiveModerationService
    {
        public const int MaxMinutes = 60 * 24 * 365;

        private readonly ILiveStreamRepository _streams;
        private readonly ILiveTrackerService _tracker;
        private readonly ILiveModerationRepository _moderation;
        private readonly IAccountRepository _accounts;

        public LiveModerationService(ILiveStreamRepository streams, ILiveTrackerService tracker, ILiveModerationRepository moderation, IAccountRepository accounts)
        {
            _streams = streams;
            _tracker = tracker;
            _moderation = moderation;
            _accounts = accounts;
        }

        private sealed record Actor(LiveStream Stream, bool IsHost, bool IsModerator);

        private async Task<Actor> ActorAsync(string connectionId, string userId, int streamId)
        {
            var stream = await _streams.GetByIdAsync(streamId);
            if (stream is null || stream.EndedAt is not null || !await _tracker.IsParticipantAsync(streamId, connectionId))
            {
                throw new NotFoundException("This stream is not live.");
            }

            var isHost = stream.HostId == userId && await _tracker.GetHostConnectionAsync(streamId) == connectionId;
            var isModerator = !isHost && await _moderation.IsModeratorAsync(stream.HostId, userId);
            return new Actor(stream, isHost, isModerator);
        }

        private async Task<AppUser> TargetAsync(string userName)
        {
            return await _accounts.GetByUserNameAsync((userName ?? string.Empty).Trim()) ?? throw new NotFoundException("That user does not exist.");
        }

        private static string Name(AppUser user)
        {
            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName;
        }

        public async Task<LiveModerationResultDto> SetModeratorAsync(string connectionId, string userId, int streamId, string userName, bool on)
        {
            var actor = await ActorAsync(connectionId, userId, streamId);
            if (!actor.IsHost)
            {
                throw new ForbiddenException("Only the person who started the stream can choose moderators.");
            }

            var target = await TargetAsync(userName);
            if (target.Id == actor.Stream.HostId)
            {
                throw new BadRequestException("You are already in charge of your own stream.");
            }

            var connections = await _tracker.GetViewerConnectionsAsync(streamId, target.Id);
            if (on)
            {
                if (connections.Count == 0)
                {
                    throw new BadRequestException("That person is not watching right now.");
                }

                // somebody who is going to look after the chat is not on the mute list any more
                await _moderation.RemoveMuteAsync(actor.Stream.HostId, target.Id);
                await _moderation.AddModeratorAsync(actor.Stream.HostId, target.Id);
            }
            else
            {
                await _moderation.RemoveModeratorAsync(actor.Stream.HostId, target.Id);
            }

            return new LiveModerationResultDto
            {
                StreamId = streamId,
                UserName = target.UserName ?? string.Empty,
                DisplayName = Name(target),
                IsModerator = on,
                TargetConnectionIds = connections
            };
        }

        public async Task<LiveModerationResultDto> MuteAsync(string connectionId, string userId, int streamId, string userName, int minutes)
        {
            var actor = await ActorAsync(connectionId, userId, streamId);
            if (!actor.IsHost && !actor.IsModerator)
            {
                throw new ForbiddenException("Only the host and the moderators can mute people in this chat.");
            }

            if (minutes < 0 || minutes > MaxMinutes)
            {
                throw new BadRequestException("Choose a time between 1 minute and 1 year, or mute for good.");
            }

            var target = await TargetAsync(userName);
            if (target.Id == actor.Stream.HostId)
            {
                throw new BadRequestException("The host cannot be muted.");
            }

            if (target.Id == userId)
            {
                throw new BadRequestException("You cannot mute yourself.");
            }

            var targetIsModerator = await _moderation.IsModeratorAsync(actor.Stream.HostId, target.Id);
            if (targetIsModerator && !actor.IsHost)
            {
                throw new ForbiddenException("Moderators cannot mute each other.");
            }

            if (targetIsModerator)
            {
                await _moderation.RemoveModeratorAsync(actor.Stream.HostId, target.Id);
            }

            DateTime? until = minutes == 0 ? null : DateTime.UtcNow.AddMinutes(minutes);
            await _moderation.SetMuteAsync(actor.Stream.HostId, target.Id, until, userId);

            // somebody who may only watch can not stay on stream as a guest
            var connections = await _tracker.GetViewerConnectionsAsync(streamId, target.Id);
            var guest = await _tracker.GetGuestAsync(streamId);
            string? endedGuest = null;
            if (guest is not null && connections.Contains(guest))
            {
                await _tracker.ClearGuestAsync(streamId);
                endedGuest = guest;
            }

            return new LiveModerationResultDto
            {
                StreamId = streamId,
                UserName = target.UserName ?? string.Empty,
                DisplayName = Name(target),
                Muted = true,
                MutedUntil = until,
                ModeratorRemoved = targetIsModerator,
                TargetConnectionIds = connections,
                EndedGuestConnectionId = endedGuest
            };
        }

        public async Task<LiveModerationResultDto> UnmuteAsync(string connectionId, string userId, int streamId, string userName)
        {
            var actor = await ActorAsync(connectionId, userId, streamId);
            if (!actor.IsHost && !actor.IsModerator)
            {
                throw new ForbiddenException("Only the host and the moderators can take people off the mute list.");
            }

            var target = await TargetAsync(userName);
            if (!await _moderation.RemoveMuteAsync(actor.Stream.HostId, target.Id))
            {
                throw new BadRequestException("That person is not on the mute list.");
            }

            return new LiveModerationResultDto
            {
                StreamId = streamId,
                UserName = target.UserName ?? string.Empty,
                DisplayName = Name(target),
                Muted = false,
                TargetConnectionIds = await _tracker.GetViewerConnectionsAsync(streamId, target.Id)
            };
        }

        public async Task<List<LiveMuteDto>> GetMutesAsync(string connectionId, string userId, int streamId)
        {
            var actor = await ActorAsync(connectionId, userId, streamId);
            if (!actor.IsHost && !actor.IsModerator)
            {
                throw new ForbiddenException("Only the host and the moderators can see the mute list.");
            }

            var rows = await _moderation.GetActiveMutesAsync(actor.Stream.HostId, DateTime.UtcNow);
            return rows.Select(row => new LiveMuteDto
            {
                StreamId = streamId,
                UserName = row.User.UserName ?? string.Empty,
                DisplayName = Name(row.User),
                AvatarUrl = row.User.AvatarUrl ?? string.Empty,
                ExpiresAt = row.ExpiresAt is null ? null : DateTime.SpecifyKind(row.ExpiresAt.Value, DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc)
            }).ToList();
        }
    }
}
