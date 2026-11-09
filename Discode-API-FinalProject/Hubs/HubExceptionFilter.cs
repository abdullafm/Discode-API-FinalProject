using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Service.Helpers.Exceptions;

namespace Discode_API_FinalProject.Hubs
{
    public class HubExceptionFilter : IHubFilter
    {
        private readonly ILogger<HubExceptionFilter> _logger;

        public HubExceptionFilter(ILogger<HubExceptionFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            _logger.LogDebug("Hub call {Method} by {User}", context.HubMethodName, context.Context.UserIdentifier);
            try
            {
                return await next(context);
            }
            catch (HubException ex)
            {
                _logger.LogInformation("Hub call {Method} by {User} was turned down: {Reason}", context.HubMethodName, context.Context.UserIdentifier, ex.Message);
                throw;
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation("Hub call {Method} by {User} was turned down: {Reason}", context.HubMethodName, context.Context.UserIdentifier, ex.Message);
                throw new HubException(ex.Message);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning("Hub call {Method} by {User} was forbidden: {Reason}", context.HubMethodName, context.Context.UserIdentifier, ex.Message);
                throw new HubException(ex.Message);
            }
            catch (BadRequestException ex)
            {
                _logger.LogInformation("Hub call {Method} by {User} was turned down: {Reason}", context.HubMethodName, context.Context.UserIdentifier, string.Join(" | ", ex.Errors));
                throw new HubException(ex.Errors.Length > 0 ? ex.Errors[0] : ex.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                Log(context, ex);
                throw new HubException("That conflicts with existing data. Refresh and try again.");
            }
            catch (Exception ex)
            {
                Log(context, ex);
                throw new HubException("Something went wrong. Please try again.");
            }
        }

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            try
            {
                await next(context);
                _logger.LogInformation("Hub connected: {User} ({Connection})", context.Context.UserIdentifier, context.Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hub connect failed for {User}", context.Context.UserIdentifier);
                context.Context.Abort();
            }
        }

        public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            try
            {
                await next(context, exception);
                _logger.LogInformation("Hub disconnected: {User} ({Connection}){Reason}", context.Context.UserIdentifier, context.Context.ConnectionId, exception is null ? string.Empty : " after " + exception.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hub disconnect cleanup failed for {User}", context.Context.UserIdentifier);
            }
        }

        private void Log(HubInvocationContext context, Exception ex)
        {
            _logger.LogError(ex, "Hub method {Method} failed for {User}", context.HubMethodName, context.Context.UserIdentifier);
        }
    }
}
