using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Helpers.Mappings;
using Service.Helpers.Validators;
using Service.Services.Interfaces;
using System.Security.Cryptography;

namespace Service.Services
{
    public class ServerService : IServerService
    {
        private const string Letters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Digits = "23456789";
        private const int CodeLength = 7;

        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IAccountRepository _accounts;
        private readonly IPresenceService _presence;
        private readonly IValidator<CreateServerDto> _createValidator;
        private readonly IValidator<JoinServerDto> _joinValidator;
        private readonly IMapper _mapper;

        public ServerService(IServerRepository servers,
                             IServerMemberRepository members,
                             IAccountRepository accounts,
                             IPresenceService presence,
                             IValidator<CreateServerDto> createValidator,
                             IValidator<JoinServerDto> joinValidator,
                             IMapper mapper)
        {
            _servers = servers;
            _members = members;
            _accounts = accounts;
            _presence = presence;
            _createValidator = createValidator;
            _joinValidator = joinValidator;
            _mapper = mapper;
        }

        public async Task<ServerChangeDto> CreateAsync(string userId, CreateServerDto model)
        {
            await _createValidator.EnsureValidAsync(model);

            if (await _accounts.GetByIdAsync(userId) is null)
            {
                throw new NotFoundException("User not found.");
            }

            var server = new Server
            {
                Name = model.Name.Trim(),
                OwnerId = userId,
                InviteCode = await NewInviteCodeAsync(),
                Members = new List<ServerMember> { new ServerMember { UserId = userId, JoinedAt = DateTime.UtcNow } }
            };
            await _servers.AddAsync(server);

            var fresh = (await _servers.GetWithMembersAsync(server.Id))!;
            return Change(fresh, userId, true);
        }

        public async Task<ServerChangeDto> JoinAsync(string userId, JoinServerDto model)
        {
            await _joinValidator.EnsureValidAsync(model);

            var server = await _servers.GetByInviteCodeAsync(ExtractCode(model.Invite));
            if (server is null)
            {
                throw new NotFoundException("That invite is invalid or has expired.");
            }

            if (server.Members.Any(m => m.UserId == userId))
            {
                return Change(server, userId, false);
            }

            await _members.AddAsync(new ServerMember { ServerId = server.Id, UserId = userId, JoinedAt = DateTime.UtcNow });
            var fresh = (await _servers.GetWithMembersAsync(server.Id))!;
            return Change(fresh, userId, true);
        }

        public async Task<IEnumerable<ServerDto>> GetMineAsync(string userId)
        {
            var servers = await _servers.GetForUserAsync(userId);
            return _mapper.Map<List<ServerDto>>(servers, ForViewer(userId));
        }

        public async Task<ServerDto> GetAsync(string userId, int serverId)
        {
            var server = await _servers.GetWithMembersAsync(serverId);
            if (server is null || server.Members.All(m => m.UserId != userId))
            {
                throw new NotFoundException("Server not found.");
            }

            return _mapper.Map<ServerDto>(server, ForViewer(userId));
        }

        public async Task<ServerChangeDto> LeaveAsync(string userId, int serverId)
        {
            var server = await _servers.GetWithMembersAsync(serverId);
            var membership = server?.Members.FirstOrDefault(m => m.UserId == userId);
            if (server is null || membership is null)
            {
                throw new NotFoundException("Server not found.");
            }

            if (server.OwnerId == userId)
            {
                throw new BadRequestException("The owner cannot leave the server.");
            }

            await _members.DeleteAsync(membership);

            var fresh = (await _servers.GetWithMembersAsync(serverId))!;
            var change = Change(fresh, userId, true);
            change.MemberIds = fresh.Members.Select(m => m.UserId).ToList();
            return change;
        }

        public async Task<ServerInviteDto> GetInviteAsync(string userId, string invite)
        {
            var server = await _servers.GetByInviteCodeAsync(ExtractCode(invite ?? string.Empty));
            if (server is null)
            {
                throw new NotFoundException("That invite is invalid or has expired.");
            }

            var online = 0;
            foreach (var member in server.Members)
            {
                if (await _presence.ResolveAsync(member.User) != "offline")
                {
                    online++;
                }
            }

            return new ServerInviteDto
            {
                ServerId = server.Id,
                Name = server.Name,
                IconUrl = server.IconUrl,
                InviteCode = server.InviteCode,
                MemberCount = server.Members.Count,
                OnlineCount = online,
                IsMember = server.Members.Any(m => m.UserId == userId),
                CreatedAt = server.CreatedAt
            };
        }

        private ServerChangeDto Change(Server server, string viewerId, bool joined)
        {
            return new ServerChangeDto
            {
                Server = _mapper.Map<ServerDto>(server, ForViewer(viewerId)),
                MemberIds = server.Members.Select(m => m.UserId).ToList(),
                Joined = joined
            };
        }

        private static string ExtractCode(string invite)
        {
            var text = invite.Trim();
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
            {
                text = uri.AbsolutePath;
            }

            var query = text.IndexOfAny(new[] { '?', '#' });
            if (query >= 0)
            {
                text = text.Substring(0, query);
            }

            return text.TrimEnd('/').Split('/').Last().Trim();
        }

        private async Task<string> NewInviteCodeAsync()
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = RandomCode();
                if (!await _servers.InviteCodeExistsAsync(code))
                {
                    return code;
                }
            }

            throw new BadRequestException("Could not create an invite link. Try again.");
        }

        private static string RandomCode()
        {
            var all = Letters + Digits;
            var chars = new char[CodeLength];
            for (var i = 0; i < CodeLength; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            var digitAt = RandomNumberGenerator.GetInt32(CodeLength);
            var letterAt = (digitAt + 1 + RandomNumberGenerator.GetInt32(CodeLength - 1)) % CodeLength;
            chars[digitAt] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            chars[letterAt] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
            return new string(chars);
        }

        private static Action<IMappingOperationOptions> ForViewer(string viewerId)
        {
            return options => options.Items[MappingProfile.ViewerIdKey] = viewerId;
        }
    }
}
