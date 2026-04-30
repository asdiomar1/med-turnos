using System.Security.Claims;
using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Filters;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace MedicalCenter.UnitTests.Filters;

public sealed class OwnershipFilterAuditTests
{
    [Fact]
    public async Task OnActionExecutionAsync_RejectsWithMismatch_LogsAuditEvent()
    {
        var logger = new TestSecurityAuditLogger();
        var filter = new OwnershipFilter(logger);
        var context = CreateContext(pacienteId: Guid.NewGuid(), userId: Guid.NewGuid());

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Should not call next"));

        Assert.Single(logger.Events);
        Assert.Equal("unauthorized_access", logger.Events[0].EventType);
    }

    [Fact]
    public async Task OnActionExecutionAsync_AllowsMatching_DoesNotLogAuditEvent()
    {
        var patientId = Guid.NewGuid();
        var logger = new TestSecurityAuditLogger();
        var filter = new OwnershipFilter(logger);
        var context = CreateContext(pacienteId: patientId, userId: patientId);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        });

        Assert.Empty(logger.Events);
        Assert.True(nextCalled);
    }

    private static ActionExecutingContext CreateContext(Guid pacienteId, Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));

        var actionDescriptor = new ActionDescriptor();
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var arguments = new Dictionary<string, object?> { ["pacienteId"] = pacienteId };

        return new ActionExecutingContext(actionContext, [], arguments, controller: null!);
    }

    private sealed class TestSecurityAuditLogger : ISecurityAuditLogger
    {
        public List<SecurityEvent> Events { get; } = [];
        public void LogAsync(SecurityEvent securityEvent) => Events.Add(securityEvent);
    }
}
