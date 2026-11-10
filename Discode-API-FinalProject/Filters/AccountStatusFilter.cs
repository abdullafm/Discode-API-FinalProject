using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.Exceptions;
using System.Security.Claims;

namespace Discode_API_FinalProject.Filters
{
    public class AccountStatusFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> WritingControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "DirectMessage", "Group", "Post", "Story", "ServerMessage"
        };

        private readonly IUserModerationRepository _moderation;
        private readonly AccountStatusCache _cache;

        public AccountStatusFilter(IUserModerationRepository moderation, AccountStatusCache cache)
        {
            _moderation = moderation;
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var principal = context.HttpContext.User;
            var userId = principal.Identity?.IsAuthenticated == true ? principal.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            if (string.IsNullOrEmpty(userId))
            {
                await next();
                return;
            }

            var status = await _cache.GetAsync(userId, _moderation);
            if (status is null)
            {
                await next();
                return;
            }

            var now = DateTime.UtcNow;
            if (status.IsDeleted || ModerationMessages.IsBanActive(status.IsBanned, status.BannedUntil, now))
            {
                throw new ForbiddenException(status.IsDeleted ? "This account has been deleted." : ModerationMessages.Banned(status.BannedUntil, status.BanReason));
            }

            if (status.ForcedLogoutAt is DateTime forced)
            {
                long.TryParse(principal.FindFirstValue("iat"), out var issuedAt);
                var issued = issuedAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(issuedAt).UtcDateTime : DateTime.MinValue;
                var kickedAt = new DateTime(forced.Ticks - forced.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
                if (issued < kickedAt)
                {
                    context.Result = new UnauthorizedObjectResult(new { isSuccess = false, errors = new[] { "Your session ended. Sign in again." } });
                    return;
                }
            }

            if (status.TimeoutUntil is DateTime until && until > now && IsWrite(context))
            {
                throw new ForbiddenException(ModerationMessages.TimedOut(until, status.TimeoutReason));
            }

            await next();
        }

        private static bool IsWrite(ActionExecutingContext context)
        {
            var method = context.HttpContext.Request.Method;
            if (method != "POST" && method != "PUT")
            {
                return false;
            }

            var controller = context.RouteData.Values["controller"] as string ?? string.Empty;
            if (!WritingControllers.Contains(controller))
            {
                return false;
            }

            return !context.HttpContext.Request.Path.Value!.EndsWith("/read", StringComparison.OrdinalIgnoreCase);
        }
    }
}
