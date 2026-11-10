using Microsoft.EntityFrameworkCore;
using Service.Helpers.Exceptions;
using System.Text.Json;

namespace Discode_API_FinalProject.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation("Not found: {Method} {Path} (user {User}): {Reason}", context.Request.Method, context.Request.Path, UserOf(context), ex.Message);
                await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning("Forbidden: {Method} {Path} (user {User}): {Reason}", context.Request.Method, context.Request.Path, UserOf(context), ex.Message);
                await WriteAsync(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (BadRequestException ex)
            {
                _logger.LogInformation("Rejected: {Method} {Path} (user {User}): {Reason}", context.Request.Method, context.Request.Path, UserOf(context), string.Join(" | ", ex.Errors));
                await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Errors);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
            }
            catch (BadHttpRequestException ex)
            {
                await WriteAsync(context, ex.StatusCode, "The request could not be read.");
            }
            catch (JsonException)
            {
                await WriteAsync(context, StatusCodes.Status400BadRequest, "The request body is not valid JSON.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status403Forbidden, "You are not allowed to do that.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status409Conflict, "This was just changed by someone else. Refresh and try again.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database rejected the change: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status409Conflict, "That conflicts with existing data. Refresh and try again.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "The service is busy right now. Please try again in a moment.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Outside service failed: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "A service we depend on is unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Method} {Path} (user {User})", context.Request.Method, context.Request.Path, UserOf(context));
                await WriteAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again later.");
            }
        }

        private static string UserOf(HttpContext context)
        {
            return context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        private static Task WriteAsync(HttpContext context, int statusCode, string message)
        {
            return WriteAsync(context, statusCode, new[] { message });
        }

        private static async Task WriteAsync(HttpContext context, int statusCode, string[] errors)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength = null;
            await context.Response.WriteAsJsonAsync(new
            {
                isSuccess = false,
                errors,
                traceId = context.TraceIdentifier
            });
        }
    }
}
