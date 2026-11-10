using Microsoft.AspNetCore.Mvc;
using Discode_API_FinalProject.Helpers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Security.Claims;

namespace Discode_API_FinalProject.Filters
{
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly ILogger _audit;

        public AuditActionFilter(ILoggerFactory loggerFactory)
        {
            _audit = loggerFactory.CreateLogger(LoggingSetup.AuditSource);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var changes = !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method);
            var timer = Stopwatch.StartNew();
            var executed = await next();
            timer.Stop();

            if (!changes)
            {
                return;
            }

            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var isAdmin = descriptor?.ControllerTypeInfo.Namespace?.EndsWith(".Admin", StringComparison.Ordinal) == true;
            var user = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var role = context.HttpContext.User.Identity?.IsAuthenticated != true ? "Anonymous" : context.HttpContext.User.IsInRole("SuperAdmin") ? "SuperAdmin" : context.HttpContext.User.IsInRole("Admin") ? "Admin" : "Member";
            var status = executed.Exception is not null && !executed.ExceptionHandled ? 500 : (executed.Result as IStatusCodeActionResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;
            var route = string.Join(", ", context.RouteData.Values.Where(v => v.Key != "controller" && v.Key != "action").Select(v => v.Key + "=" + v.Value));
            var arguments = isAdmin
                ? string.Join("; ", context.ActionArguments.Select(a => a.Key + ": " + LogRedactor.Describe(a.Value)))
                : string.Empty;

            _audit.LogInformation(
                "{Role} {User} {Method} {Controller}.{Action} [{Route}] -> {Status} in {Ms} ms {Arguments}",
                role, user, request.Method, descriptor?.ControllerName, descriptor?.ActionName, route, status, timer.ElapsedMilliseconds, arguments);
        }
    }
}
