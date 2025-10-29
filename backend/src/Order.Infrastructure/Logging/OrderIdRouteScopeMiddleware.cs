using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing; 
using Order.Core.Logging;

namespace Order.Api.Infrastructure.Logging;

public sealed class OrderIdRouteScopeMiddleware(RequestDelegate next, ILogger<OrderIdRouteScopeMiddleware> logger)
{
    public async Task Invoke(HttpContext ctx)
    {
        var routeData = ctx.GetRouteData();

        if (routeData?.Values is { } values &&
            values.TryGetValue("id", out var raw) &&
            raw is string id &&
            !string.IsNullOrWhiteSpace(id))
        {
            using (logger.BeginScope(new Dictionary<string, object> { [Correlation.Key] = id }))
            {
                await next(ctx);
                return;
            }
        }

        await next(ctx);
    }
}
