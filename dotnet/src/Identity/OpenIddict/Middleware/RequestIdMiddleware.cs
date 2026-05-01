using Microsoft.AspNetCore.Http;

namespace AQ.Identity.OpenIddict.Middleware;

public class RequestIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        await next(context);
    }
}
