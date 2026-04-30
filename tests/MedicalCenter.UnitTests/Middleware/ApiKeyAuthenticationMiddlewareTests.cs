using MedicalCenter.Api.Middleware;
using MedicalCenter.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MedicalCenter.UnitTests.Middleware;

public sealed class ApiKeyAuthenticationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ValidApiKey_SetsUserAndCallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware("secret123", async context =>
        {
            nextCalled = true;
            Assert.True(context.User.Identity?.IsAuthenticated);
            Assert.Contains(context.User.Claims, c => c.Value == "whatsapp.dispatch");
            await Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/whatsapp/dispatch", apiKey: "secret123");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidApiKey_Returns401()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware("secret123", _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/whatsapp/dispatch", apiKey: "wrong-key");

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingApiKey_Returns401()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware("secret123", _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/whatsapp/dispatch", apiKey: null);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NonWebhookPath_SkipsAuthAndCallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware("secret123", _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/pacientes", apiKey: null);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static ApiKeyAuthenticationMiddleware CreateMiddleware(string key, RequestDelegate next)
    {
        var options = Options.Create(new ApiKeyOptions { Key = key });
        return new ApiKeyAuthenticationMiddleware(next, options);
    }

    private static DefaultHttpContext CreateContext(string path, string? apiKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (apiKey is not null)
        {
            context.Request.Headers["X-Api-Key"] = apiKey;
        }
        context.Response.Body = new MemoryStream();
        return context;
    }
}
