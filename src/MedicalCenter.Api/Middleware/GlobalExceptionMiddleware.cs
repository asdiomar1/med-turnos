using System.Net;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Contracts.Common;

namespace MedicalCenter.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
            await WriteErrorAsync(context, exception);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, new ApiErrorResponse { Error = validation.Message, Code = validation.Code, Details = validation.Details }),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, new ApiErrorResponse { Error = unauthorized.Message, Code = unauthorized.Code }),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, new ApiErrorResponse { Error = forbidden.Message, Code = forbidden.Code }),
            NotFoundException notFound => (HttpStatusCode.NotFound, new ApiErrorResponse { Error = notFound.Message, Code = notFound.Code }),
            ConflictException conflict => (HttpStatusCode.Conflict, new ApiErrorResponse { Error = conflict.Message, Code = conflict.Code }),
            FeatureNotImplementedException notImplemented => (HttpStatusCode.NotImplemented, new ApiErrorResponse { Error = notImplemented.Message, Code = notImplemented.Code }),
            _ => (HttpStatusCode.InternalServerError, new ApiErrorResponse { Error = "Error interno del servidor", Code = "internal_error" })
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
