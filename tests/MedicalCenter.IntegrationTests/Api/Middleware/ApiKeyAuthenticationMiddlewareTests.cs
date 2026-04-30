using MedicalCenter.Api.Middleware;
using MedicalCenter.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedicalCenter.IntegrationTests.Api.Middleware;

public sealed class ApiKeyAuthenticationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_MissingApiKeyOnWebhookPath_Returns401()
    {
        var options = Options.Create(new ApiKeyOptions { Key = "valid-key" });
        var middleware = new ApiKeyAuthenticationMiddleware(next: _ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/whatsapp/dispatch";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidApiKeyOnWebhookPath_Returns401()
    {
        var options = Options.Create(new ApiKeyOptions { Key = "valid-key" });
        var middleware = new ApiKeyAuthenticationMiddleware(next: _ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/whatsapp/dispatch";
        context.Request.Headers["X-Api-Key"] = "invalid-key";

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidApiKeyOnWebhookPath_AuthenticatesAndContinues()
    {
        var options = Options.Create(new ApiKeyOptions { Key = "valid-key" });
        var executed = false;
        var middleware = new ApiKeyAuthenticationMiddleware(next: _ => { executed = true; return Task.CompletedTask; }, options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/whatsapp/dispatch";
        context.Request.Headers["X-Api-Key"] = "valid-key";

        await middleware.InvokeAsync(context);

        Assert.True(executed);
        Assert.True(context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_NonWebhookPath_SkipsCheck()
    {
        var options = Options.Create(new ApiKeyOptions { Key = "valid-key" });
        var executed = false;
        var middleware = new ApiKeyAuthenticationMiddleware(next: _ => { executed = true; return Task.CompletedTask; }, options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/pacientes";

        await middleware.InvokeAsync(context);

        Assert.True(executed);
        Assert.Equal(200, context.Response.StatusCode);
    }
}
