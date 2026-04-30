using System.Security.Claims;
using MedicalCenter.Api.Filters;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace MedicalCenter.UnitTests.Filters;

public sealed class OwnershipFilterTests
{
    private static readonly ISecurityAuditLogger NoOpLogger = new NoOpSecurityAuditLogger();

    [Fact]
    public async Task OnActionExecutionAsync_PacienteIdMatchesUserId_AllowsRequest()
    {
        var patientId = Guid.NewGuid();
        var filter = new OwnershipFilter(NoOpLogger);
        var context = CreateContext(pacienteId: patientId, userId: patientId);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        });

        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_PacienteIdMismatchesUserId_Returns403()
    {
        var filter = new OwnershipFilter(NoOpLogger);
        var context = CreateContext(pacienteId: Guid.NewGuid(), userId: Guid.NewGuid());

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Should not call next"));

        Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, (context.Result as ObjectResult)?.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoPacienteIdArgument_AllowsRequest()
    {
        var filter = new OwnershipFilter(NoOpLogger);
        var context = CreateContext(pacienteId: null, userId: Guid.NewGuid());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        });

        Assert.Null(context.Result);
        Assert.True(nextCalled);
    }

    private static ActionExecutingContext CreateContext(Guid? pacienteId, Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));

        var actionDescriptor = new ActionDescriptor();
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var arguments = new Dictionary<string, object?>();
        if (pacienteId.HasValue)
        {
            arguments["pacienteId"] = pacienteId.Value;
        }

        return new ActionExecutingContext(actionContext, [], arguments, controller: null!);
    }

    private sealed class NoOpSecurityAuditLogger : ISecurityAuditLogger
    {
        public void LogAsync(SecurityEvent securityEvent) { }
    }
}
