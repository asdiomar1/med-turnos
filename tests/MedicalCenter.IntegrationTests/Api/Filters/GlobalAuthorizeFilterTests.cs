using MedicalCenter.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace MedicalCenter.IntegrationTests.Api.Filters;

public sealed class GlobalAuthorizeFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_UnauthenticatedUser_Returns401()
    {
        var filter = new GlobalAuthorizeFilter();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
        Assert.Equal("Bearer", httpContext.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public async Task OnAuthorizationAsync_AllowAnonymous_SkipsAuthorization()
    {
        var filter = new GlobalAuthorizeFilter();
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = new List<object> { new AllowAnonymousAttribute() }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }
}
