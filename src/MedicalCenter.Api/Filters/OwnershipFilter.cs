using MedicalCenter.Api.Extensions;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicalCenter.Api.Filters;

public sealed class OwnershipFilter(ISecurityAuditLogger auditLogger) : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.TryGetValue("pacienteId", out var value) && value is Guid pacienteId)
        {
            var userId = context.HttpContext.User.GetUserId();
            if (pacienteId != userId)
            {
                auditLogger.LogAsync(new SecurityEvent(
                    "unauthorized_access",
                    $"User {userId} attempted to access patient {pacienteId}",
                    userId.ToString(),
                    context.HttpContext.Request.Path));

                context.Result = new ObjectResult(new { error = "Forbidden" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return Task.CompletedTask;
            }
        }

        return next();
    }
}
