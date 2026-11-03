using Microsoft.EntityFrameworkCore;
using Service.Helpers.Exceptions;
using System.Text.Json;

namespace Discode_API_FinalProject.Middlewares
{
    // The one place where an exception becomes an HTTP answer. Every response has the same shape: { isSuccess:false, errors:[...], traceId }
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
                await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await WriteAsync(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (BadRequestException ex)
            {
                await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Errors);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // the client went away, nobody is waiting for an answer
            }
            catch (BadHttpRequestException ex)
            {
                // oversized body, broken framing, unreadable request
                await WriteAsync(context, ex.StatusCode, "The request could not be read.");
            }
            catch (JsonException)
            {
                await WriteAsync(context, StatusCodes.Status400BadRequest, "The request body is not valid JSON.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status403Forbidden, "You are not allowed to do that.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // somebody else changed or removed the same row between our read and our write
                _logger.LogWarning(ex, "Concurrency conflict: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status409Conflict, "This was just changed by someone else. Refresh and try again.");
            }
            catch (DbUpdateException ex)
            {
                // unique index, foreign key or length limit hit at the database
                _logger.LogWarning(ex, "Database rejected the change: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status409Conflict, "That conflicts with existing data. Refresh and try again.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "The service is busy right now. Please try again in a moment.");
            }
            catch (HttpRequestException ex)
            {
                // an outside service the API depends on (e-mail provider) is down
                _logger.LogError(ex, "Outside service failed: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "A service we depend on is unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again later.");
            }
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

            // keep CORS and other headers set earlier in the pipeline, only the body and status change
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
