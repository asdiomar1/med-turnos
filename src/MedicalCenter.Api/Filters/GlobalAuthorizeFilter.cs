using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicalCenter.Api.Filters;

public sealed class GlobalAuthorizeFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
        {
            return Task.CompletedTask;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.HttpContext.Response.Headers.WWWAuthenticate = "Bearer";
            context.Result = new UnauthorizedResult();
        }

        return Task.CompletedTask;
    }
}
