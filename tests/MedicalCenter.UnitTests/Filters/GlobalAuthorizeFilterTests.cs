using System.Security.Claims;
using MedicalCenter.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace MedicalCenter.UnitTests.Filters;

public sealed class GlobalAuthorizeFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_UserNotAuthenticated_Returns401()
    {
        var filter = new GlobalAuthorizeFilter();
        var context = CreateContext(authenticated: false);

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_UserAuthenticated_AllowsRequest()
    {
        var filter = new GlobalAuthorizeFilter();
        var context = CreateContext(authenticated: true);

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_AllowAnonymousAttribute_AllowsUnauthenticatedRequest()
    {
        var filter = new GlobalAuthorizeFilter();
        var context = CreateContext(authenticated: false, allowAnonymous: true);

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext(bool authenticated, bool allowAnonymous = false)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"));
        }

        var actionDescriptor = new ActionDescriptor();
        if (allowAnonymous)
        {
            actionDescriptor.EndpointMetadata = [new AllowAnonymousAttribute()];
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, []);
    }
}
