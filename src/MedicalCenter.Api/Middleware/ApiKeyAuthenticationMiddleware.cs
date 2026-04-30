using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace MedicalCenter.Api.Middleware;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next, IOptions<ApiKeyOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1/whatsapp"))
        {
            await next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey) || !string.Equals(apiKey, options.Value.Key, StringComparison.Ordinal))
        {
            await RejectAsync(context);
            return;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "service"),
            new Claim("permission", "whatsapp.dispatch"),
            new Claim("permission", "config.whatsapp.manage")
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
        await next(context);
    }

    private static async Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
    }
}
