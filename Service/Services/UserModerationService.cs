using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class UserModerationService : IUserModerationService
    {
        private readonly IUserModerationRepository _moderation;
        private readonly IAdminRepository _admin;
        private readonly IValidator<WarnUserDto> _warnValidator;
        private readonly IValidator<TimeoutUserDto> _timeoutValidator;
        private readonly IValidator<KickUserDto> _kickValidator;
        private readonly IValidator<BanUserDto> _banValidator;
        private readonly IValidator<DeleteUserDto> _deleteValidator;
        private readonly IValidator<AdminListFilterDto> _filterValidator;

        public UserModerationService(IUserModerationRepository moderation,
                                     IAdminRepository admin,
                                     IValidator<WarnUserDto> warnValidator,
                                     IValidator<TimeoutUserDto> timeoutValidator,
                                     IValidator<KickUserDto> kickValidator,
                                     IValidator<BanUserDto> banValidator,
                                     IValidator<DeleteUserDto> deleteValidator,
                                     IValidator<AdminListFilterDto> filterValidator)
        {
            _moderation = moderation;
            _admin = admin;
            _warnValidator = warnValidator;
            _timeoutValidator = timeoutValidator;
            _kickValidator = kickValidator;
            _banValidator = banValidator;
            _deleteValidator = deleteValidator;
            _filterValidator = filterValidator;
        }

        public async Task<AdminUserDetailDto> GetAsync(ModerationActor actor, string userId)
        {
            var user = await _moderation.GetTrackedUserAsync(userId) ?? throw new NotFoundException("Member not found.");
            var roles = (await _admin.GetRolesForUsersAsync(new List<string> { userId })).GetValueOrDefault(userId) ?? new List<string>();
            var servers = await _admin.GetServerCountsForUsersAsync(new List<string> { userId });
            var bans = await _admin.GetBanCountsForUsersAsync(new List<string> { userId });
            var warnings = await _moderation.GetWarningsAsync(userId, 20);
            var history = await _moderation.GetActionsForTargetAsync(AdminTargetType.User, userId, 20);
            var now = DateTime.UtcNow;

            var banned = ModerationMessages.IsBanActive(user.IsBanned, user.BannedUntil, now);
            var allowed = CanActOn(actor, userId, roles);

            return new AdminUserDetailDto
            {
                Id = user.Id,
                PublicId = user.PublicId,
                UserName = user.UserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                IsPublic = user.IsPublic,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                IsDeleted = user.IsDeleted,
                IsBanned = banned,
                BannedUntil = banned ? user.BannedUntil : null,
                BannedAt = banned ? user.BannedAt : null,
                BanReason = banned ? user.BanReason : string.Empty,
                BannedByUserName = banned && user.BannedById is not null ? await _admin.GetUserNameAsync(user.BannedById) ?? string.Empty : string.Empty,
                TimedOut = user.TimeoutUntil is not null && user.TimeoutUntil > now,
                TimeoutUntil = user.TimeoutUntil is not null && user.TimeoutUntil > now ? user.TimeoutUntil : null,
                TimeoutReason = user.TimeoutUntil is not null && user.TimeoutUntil > now ? user.TimeoutReason : string.Empty,
                ServersJoined = servers.GetValueOrDefault(userId),
                BansReceived = bans.GetValueOrDefault(userId),
                Warnings = await _moderation.CountWarningsAsync(userId),
                ReportsAgainst = await _moderation.CountReportsAgainstAsync(userId),
                Posts = await _moderation.CountPostsByAsync(userId),
                Comments = await _moderation.CountCommentsByAsync(userId),
                CanModerate = allowed && !user.IsDeleted,
                CanDelete = allowed && actor.IsSuperAdmin && !user.IsDeleted,
                LiveBanned = ModerationMessages.IsLiveBanActive(user.LiveBanned, user.LiveBannedUntil, now),
                LiveBannedUntil = ModerationMessages.IsLiveBanActive(user.LiveBanned, user.LiveBannedUntil, now) ? user.LiveBannedUntil : null,
                LiveBanReason = ModerationMessages.IsLiveBanActive(user.LiveBanned, user.LiveBannedUntil, now) ? user.LiveBanReason : string.Empty,
                CanChangeRole = actor.IsSuperAdmin && userId != actor.UserId && !user.IsDeleted,
                RecentWarnings = warnings.Select(x => new UserWarningDto
                {
                    Id = x.Warning.Id,
                    Reason = x.Warning.Reason,
                    IssuedByUserName = x.IssuedBy,
                    CreatedAt = x.Warning.CreatedAt,
                    Seen = x.Warning.SeenAt is not null
                }).ToList(),
                History = history.Select(ToDto).ToList()
            };
        }

        public async Task<ModerationResultDto> WarnAsync(ModerationActor actor, string userId, WarnUserDto model)
        {
            await _warnValidator.EnsureValidAsync(model);
            var user = await LoadForActionAsync(actor, userId);
            var reason = model.Reason.Trim();

            var warning = new UserWarning { UserId = userId, IssuedById = actor.UserId, Reason = reason };
            await _moderation.AddWarningAsync(warning);
            await LogAsync(actor, user, AdminActionKind.Warn, reason, string.Empty);
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto
            {
                UserId = userId,
                Reason = reason,
                WarningId = warning.Id,
                WarningCount = await _moderation.CountWarningsAsync(userId)
            };
        }

        public async Task<ModerationResultDto> TimeoutAsync(ModerationActor actor, string userId, TimeoutUserDto model)
        {
            await _timeoutValidator.EnsureValidAsync(model);
            var user = await LoadForActionAsync(actor, userId);
            var reason = model.Reason.Trim();

            user.TimeoutUntil = DateTime.UtcNow.AddMinutes(model.Minutes);
            user.TimeoutReason = reason;
            await LogAsync(actor, user, AdminActionKind.Timeout, reason, DescribeMinutes(model.Minutes));
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, Reason = reason, TimeoutUntil = user.TimeoutUntil };
        }

        public async Task<ModerationResultDto> ClearTimeoutAsync(ModerationActor actor, string userId)
        {
            var user = await LoadForActionAsync(actor, userId);
            if (user.TimeoutUntil is null || user.TimeoutUntil <= DateTime.UtcNow)
            {
                throw new BadRequestException("That member is not timed out.");
            }

            user.TimeoutUntil = null;
            user.TimeoutReason = string.Empty;
            await LogAsync(actor, user, AdminActionKind.ClearTimeout, "Timeout removed", string.Empty);
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId };
        }

        public async Task<ModerationResultDto> ChangeRoleAsync(ModerationActor actor, string userId, ChangeRoleDto model)
        {
            if (!actor.IsSuperAdmin)
            {
                throw new ForbiddenException("Only a SuperAdmin can change roles.");
            }

            var role = (model.Role ?? string.Empty).Trim();
            if (!Roles.All.Contains(role))
            {
                throw new BadRequestException("Choose Member, Admin or SuperAdmin.");
            }

            if (userId == actor.UserId)
            {
                throw new ForbiddenException("You cannot change your own role.");
            }

            var user = await _moderation.GetTrackedUserAsync(userId) ?? throw new NotFoundException("Member not found.");
            if (user.IsDeleted)
            {
                throw new BadRequestException("That account is deleted.");
            }

            var current = (await _admin.GetRolesForUsersAsync(new List<string> { userId })).GetValueOrDefault(userId)?.FirstOrDefault() ?? Roles.Member;
            if (current == role)
            {
                throw new BadRequestException("They already have that role.");
            }

            await _moderation.SetRoleAsync(userId, role);

            // the role lives inside the sign-in token, so the old token has to stop working
            user.ForcedLogoutAt = DateTime.UtcNow;
            await LogAsync(actor, user, AdminActionKind.ChangeRole, current + " to " + role, "Signed out to apply the new role");
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, ForceLogout = true, Reason = "Your role changed. Sign in again." };
        }

        public async Task<ModerationResultDto> LiveBanAsync(ModerationActor actor, string userId, LiveBanDto model)
        {
            var reason = (model.Reason ?? string.Empty).Trim();
            if (reason.Length == 0 || reason.Length > 300)
            {
                throw new BadRequestException("Write a reason of up to 300 characters.");
            }

            if (model.Minutes != 0 && model.Minutes != 60 && model.Minutes != 1440 && model.Minutes != 10080)
            {
                throw new BadRequestException("Choose 1 hour, 1 day, 7 days or permanent.");
            }

            var user = await LoadForActionAsync(actor, userId);
            var now = DateTime.UtcNow;
            if (ModerationMessages.IsLiveBanActive(user.LiveBanned, user.LiveBannedUntil, now))
            {
                throw new BadRequestException("That member already cannot go live.");
            }

            user.LiveBanned = true;
            user.LiveBannedUntil = model.Minutes > 0 ? now.AddMinutes(model.Minutes) : null;
            user.LiveBanReason = reason;
            await LogAsync(actor, user, AdminActionKind.LiveBan, reason, model.Minutes > 0 ? DescribeMinutes(model.Minutes) : "Permanent");
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, Reason = reason, TimeoutUntil = user.LiveBannedUntil };
        }

        public async Task<ModerationResultDto> LiveUnbanAsync(ModerationActor actor, string userId)
        {
            var user = await LoadForActionAsync(actor, userId);
            if (!ModerationMessages.IsLiveBanActive(user.LiveBanned, user.LiveBannedUntil, DateTime.UtcNow))
            {
                throw new BadRequestException("That member can already go live.");
            }

            user.LiveBanned = false;
            user.LiveBannedUntil = null;
            user.LiveBanReason = string.Empty;
            await LogAsync(actor, user, AdminActionKind.LiveUnban, "Live ban removed", string.Empty);
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId };
        }

        public async Task<ModerationResultDto> LogLiveEndAsync(ModerationActor actor, string userId, string reason, string streamTitle)
        {
            var text = (reason ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > 300)
            {
                throw new BadRequestException("Write a reason of up to 300 characters.");
            }

            var user = await LoadForActionAsync(actor, userId);
            await LogAsync(actor, user, AdminActionKind.LiveEnd, text, "Stream: " + streamTitle);
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, Reason = text };
        }

        public async Task<ModerationResultDto> KickAsync(ModerationActor actor, string userId, KickUserDto model)
        {
            await _kickValidator.EnsureValidAsync(model);
            var user = await LoadForActionAsync(actor, userId);
            var reason = model.Reason.Trim();

            user.ForcedLogoutAt = DateTime.UtcNow;
            await LogAsync(actor, user, AdminActionKind.Kick, reason, "Signed out of every device");
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, ForceLogout = true, Reason = reason };
        }

        public async Task<ModerationResultDto> BanAsync(ModerationActor actor, string userId, BanUserDto model)
        {
            await _banValidator.EnsureValidAsync(model);
            var user = await LoadForActionAsync(actor, userId);
            var now = DateTime.UtcNow;
            if (ModerationMessages.IsBanActive(user.IsBanned, user.BannedUntil, now))
            {
                throw new BadRequestException("That member is already banned.");
            }

            var reason = model.Reason.Trim();
            user.IsBanned = true;
            user.BannedUntil = model.Days > 0 ? now.AddDays(model.Days) : null;
            user.BannedAt = now;
            user.BanReason = reason;
            user.BannedById = actor.UserId;
            user.ForcedLogoutAt = now;
            await LogAsync(actor, user, AdminActionKind.Ban, reason, model.Days > 0 ? model.Days + (model.Days == 1 ? " day" : " days") : "Permanent");
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, ForceLogout = true, Reason = reason };
        }

        public async Task<ModerationResultDto> UnbanAsync(ModerationActor actor, string userId)
        {
            var user = await LoadForActionAsync(actor, userId);
            if (!ModerationMessages.IsBanActive(user.IsBanned, user.BannedUntil, DateTime.UtcNow))
            {
                throw new BadRequestException("That member is not banned.");
            }

            user.IsBanned = false;
            user.BannedUntil = null;
            user.BannedAt = null;
            user.BanReason = string.Empty;
            user.BannedById = null;
            await LogAsync(actor, user, AdminActionKind.Unban, "Ban removed", string.Empty);
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId };
        }

        public async Task<ModerationResultDto> DeleteAsync(ModerationActor actor, string userId, DeleteUserDto model)
        {
            await _deleteValidator.EnsureValidAsync(model);
            if (!actor.IsSuperAdmin)
            {
                throw new ForbiddenException("Only a SuperAdmin can delete an account.");
            }

            var user = await LoadForActionAsync(actor, userId);
            if (!string.Equals(model.UserName.Trim(), user.UserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The username you typed does not match.");
            }

            var reason = model.Reason.Trim();
            var original = user.UserName ?? userId;
            var tag = user.Id.Replace("-", string.Empty);
            tag = tag.Length > 10 ? tag.Substring(0, 10) : tag;

            // the account is removed but its content keeps working, so the personal data is scrubbed instead of dropping rows
            user.IsDeleted = true;
            user.IsBanned = true;
            user.BannedUntil = null;
            user.BannedAt = DateTime.UtcNow;
            user.BanReason = "Account deleted";
            user.BannedById = actor.UserId;
            user.ForcedLogoutAt = DateTime.UtcNow;
            user.UserName = "deleted_" + tag;
            user.NormalizedUserName = user.UserName.ToUpperInvariant();
            user.Email = "deleted_" + tag + "@deleted.invalid";
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.DisplayName = "Deleted user";
            user.PasswordHash = null;
            user.AvatarUrl = string.Empty;
            user.Bio = string.Empty;
            user.Pronouns = string.Empty;
            user.CustomStatus = string.Empty;
            user.PhoneNumber = null;

            await _moderation.AddActionAsync(new AdminAction
            {
                AdminId = actor.UserId,
                TargetType = AdminTargetType.User,
                TargetId = userId,
                TargetName = original,
                Kind = AdminActionKind.Delete,
                Reason = reason,
                Details = "Account deleted"
            });
            await _moderation.SaveChangesAsync();

            return new ModerationResultDto { UserId = userId, ForceLogout = true, Reason = reason };
        }

        public async Task<AdminActionListDto> GetActivityAsync(AdminListFilterDto filter)
        {
            await _filterValidator.EnsureValidAsync(filter);
            var (actions, total) = await _moderation.GetActionsAsync(filter.Search, filter.Page, filter.PageSize);
            return new AdminActionListDto
            {
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = actions.Select(ToDto).ToList()
            };
        }

        public async Task<List<UnseenWarningDto>> GetUnseenWarningsAsync(string userId)
        {
            var warnings = await _moderation.GetUnseenWarningsAsync(userId);
            return warnings.Select(x => new UnseenWarningDto { Id = x.Id, Reason = x.Reason, CreatedAt = x.CreatedAt }).ToList();
        }

        public Task AcknowledgeWarningAsync(string userId, int warningId)
        {
            return _moderation.MarkWarningSeenAsync(userId, warningId);
        }

        private async Task<AppUser> LoadForActionAsync(ModerationActor actor, string userId)
        {
            var user = await _moderation.GetTrackedUserAsync(userId) ?? throw new NotFoundException("Member not found.");
            var roles = (await _admin.GetRolesForUsersAsync(new List<string> { userId })).GetValueOrDefault(userId) ?? new List<string>();

            if (userId == actor.UserId)
            {
                throw new ForbiddenException("You cannot do that to your own account.");
            }

            if (roles.Contains(Roles.SuperAdmin))
            {
                throw new ForbiddenException("A SuperAdmin account cannot be moderated.");
            }

            if (roles.Contains(Roles.Admin) && !actor.IsSuperAdmin)
            {
                throw new ForbiddenException("Only a SuperAdmin can moderate an Admin.");
            }

            if (user.IsDeleted)
            {
                throw new BadRequestException("That account is already deleted.");
            }

            return user;
        }

        private static bool CanActOn(ModerationActor actor, string userId, List<string> roles)
        {
            if (userId == actor.UserId || roles.Contains(Roles.SuperAdmin))
            {
                return false;
            }

            return !roles.Contains(Roles.Admin) || actor.IsSuperAdmin;
        }

        private async Task LogAsync(ModerationActor actor, AppUser user, AdminActionKind kind, string reason, string details)
        {
            await _moderation.AddActionAsync(new AdminAction
            {
                AdminId = actor.UserId,
                TargetType = AdminTargetType.User,
                TargetId = user.Id,
                TargetName = user.UserName ?? user.Id,
                Kind = kind,
                Reason = reason,
                Details = details
            });
        }

        private static string DescribeMinutes(int minutes)
        {
            return minutes switch
            {
                10 => "10 minutes",
                60 => "1 hour",
                1440 => "1 day",
                10080 => "7 days",
                _ => minutes + " minutes"
            };
        }

        private static AdminActionDto ToDto(AdminAction action)
        {
            return new AdminActionDto
            {
                Id = action.Id,
                AdminUserName = action.Admin?.UserName ?? string.Empty,
                TargetType = action.TargetType.ToString(),
                TargetId = action.TargetId,
                TargetName = action.TargetName,
                Kind = action.Kind.ToString(),
                Reason = action.Reason,
                Details = action.Details,
                CreatedAt = action.CreatedAt
            };
        }
    }
}
