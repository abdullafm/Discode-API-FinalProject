using Microsoft.AspNetCore.Mvc.Filters;
using Repository.Repositories.Interfaces;
using Service.Helpers.Exceptions;

namespace Discode_API_FinalProject.Filters
{
    public class BannedServerFilter : IAsyncActionFilter
    {
        private readonly IServerRepository _servers;

        public BannedServerFilter(IServerRepository servers)
        {
            _servers = servers;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var values = context.RouteData.Values;
            object? raw = null;
            if (values.TryGetValue("serverId", out var serverId))
            {
                raw = serverId;
            }
            else if (string.Equals(values["controller"] as string, "Server", StringComparison.OrdinalIgnoreCase) && values.TryGetValue("id", out var id))
            {
                raw = id;
            }

            if (raw is not null && int.TryParse(raw.ToString(), out var parsed) && await _servers.IsBannedAsync(parsed))
            {
                throw new ForbiddenException("This server has been banned.");
            }

            await next();
        }
    }
}
