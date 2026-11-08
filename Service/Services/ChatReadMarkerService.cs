using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ReadMarkers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ChatReadMarkerService : IChatReadMarkerService
    {
        private static readonly string[] Kinds = { "dm", "group", "channel" };

        private readonly IChatReadMarkerRepository _markers;

        public ChatReadMarkerService(IChatReadMarkerRepository markers)
        {
            _markers = markers;
        }

        public async Task<ReadMarkerDto> GetAsync(string userId, string kind, string key)
        {
            (kind, key) = Clean(kind, key);
            var found = await _markers.GetAsync(userId, kind, key);
            return new ReadMarkerDto { ReadAt = found is null ? null : DateTime.SpecifyKind(found.ReadAt, DateTimeKind.Utc) };
        }

        public async Task<ReadMarkerDto> MarkAsync(string userId, MarkReadDto model)
        {
            var (kind, key) = Clean(model?.Kind, model?.Key);
            var now = DateTime.UtcNow;
            var found = await _markers.GetAsync(userId, kind, key);
            if (found is null)
            {
                await _markers.AddAsync(new ChatReadMarker { UserId = userId, Kind = kind, Key = key, ReadAt = now });
            }
            else
            {
                found.ReadAt = now;
                await _markers.UpdateAsync(found);
            }

            return new ReadMarkerDto { ReadAt = DateTime.SpecifyKind(now, DateTimeKind.Utc) };
        }

        private static (string Kind, string Key) Clean(string? kind, string? key)
        {
            kind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            key = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (!Kinds.Contains(kind) || key.Length == 0 || key.Length > 100)
            {
                throw new BadRequestException("Choose a chat.");
            }

            return (kind, key);
        }
    }
}
