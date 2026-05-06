namespace MedicalCenter.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ContentSecurityPolicy =
        "default-src 'none'; " +
        "base-uri 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "object-src 'none'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "connect-src 'self'";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        context.Response.Headers.XXSSProtection = "1; mode=block";

        await next(context);
    }
}
