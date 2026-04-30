using MedicalCenter.Api.Extensions;
using MedicalCenter.Api.Mappings;
using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Contracts.Auth;
using MedicalCenter.Contracts.Common;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, ISecurityAuditLogger auditLogger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.LoginAsync(request.Identifier, request.Password, cancellationToken);
            return Ok(result.ToResponse());
        }
        catch (UnauthorizedException)
        {
            auditLogger.LogAsync(new SecurityEvent(
                EventType: "auth_failure",
                Message: $"Failed login attempt for identifier: {request.Identifier}",
                UserId: request.Identifier,
                Path: "/api/v1/auth/login",
                IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            ));
            throw;
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("portal/sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> PortalSignIn([FromBody] LoginRequest request, CancellationToken cancellationToken) => Login(request, cancellationToken);

    [HttpPost("portal/activate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalActivationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivatePortal([FromBody] PortalActivateRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ActivatePortalAsync(request.Token, request.LoginIdentifier, request.Password, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPost("portal/recovery")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalRecoveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPortalRecovery([FromBody] PortalRecoveryRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPortalRecoveryAsync(request.DocumentoIdentidad, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPost("portal/access-tokens")]
    [Authorize]
    [ProducesResponseType(typeof(DataResponse<PortalAccessTokenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePortalAccessToken([FromBody] CreatePortalAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.CreatePortalAccessTokenAsync(request.PacienteId, request.Purpose, request.DeliveryChannel, User.GetUserId(), cancellationToken);
        return Ok(new DataResponse<PortalAccessTokenResponse>
        {
            Data = result.ToResponse(),
            Error = null
        });
    }

    [HttpGet("me/effective-access")]
    [Authorize]
    [ProducesResponseType(typeof(EffectiveAccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffectiveAccess(CancellationToken cancellationToken)
    {
        var result = await authService.GetEffectiveAccessAsync(User.GetUserId(), cancellationToken);
        return Ok(result.ToResponse());
    }
}
