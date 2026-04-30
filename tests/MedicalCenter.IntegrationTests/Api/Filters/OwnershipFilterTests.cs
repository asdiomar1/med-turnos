using MedicalCenter.Api.Filters;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace MedicalCenter.IntegrationTests.Api.Filters;

public sealed class OwnershipFilterTests
{
    private static readonly ISecurityAuditLogger NoOpLogger = new NoOpAuditLogger();

    [Fact]
    public async Task OnActionExecutionAsync_MismatchedPacienteId_Returns403()
    {
        var filter = new OwnershipFilter(NoOpLogger);
        var userId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"))
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var actionArguments = new Dictionary<string, object?> { { "pacienteId", pacienteId } };
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), actionArguments, controller: null!);

        var executed = false;
        ActionExecutionDelegate next = () =>
        {
            executed = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.False(executed);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_MatchingPacienteId_ContinuesExecution()
    {
        var filter = new OwnershipFilter(NoOpLogger);
        var userId = Guid.NewGuid();
        var pacienteId = userId;

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"))
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var actionArguments = new Dictionary<string, object?> { { "pacienteId", pacienteId } };
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), actionArguments, controller: null!);

        var executed = false;
        ActionExecutionDelegate next = () =>
        {
            executed = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(executed);
        Assert.Null(context.Result);
    }

    private sealed class NoOpAuditLogger : ISecurityAuditLogger
    {
        public void LogAsync(SecurityEvent securityEvent) { }
    }
}
