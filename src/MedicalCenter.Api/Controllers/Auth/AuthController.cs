using MedicalCenter.Api.Extensions;
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
            return Ok(Map(result));
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
        return Ok(Map(result));
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
        return Ok(new PortalActivationResponse
        {
            Ok = result.Ok,
            LoginIdentifier = result.LoginIdentifier
        });
    }

    [HttpPost("portal/recovery")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PortalRecoveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPortalRecovery([FromBody] PortalRecoveryRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPortalRecoveryAsync(request.DocumentoIdentidad, cancellationToken);
        return Ok(new PortalRecoveryResponse
        {
            Ok = result.Ok,
            NeedsManualSupport = result.NeedsManualSupport
        });
    }

    [HttpPost("portal/access-tokens")]
    [Authorize]
    [ProducesResponseType(typeof(DataResponse<PortalAccessTokenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePortalAccessToken([FromBody] CreatePortalAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.CreatePortalAccessTokenAsync(request.PacienteId, request.Purpose, request.DeliveryChannel, User.GetUserId(), cancellationToken);
        var response = new PortalAccessTokenResponse
        {
            TokenId = result.TokenId,
            Purpose = result.Purpose,
            DeliveryChannel = result.DeliveryChannel,
            ExpiresAt = result.ExpiresAt,
            TokenPlain = result.TokenPlain
        };

        return Ok(new DataResponse<PortalAccessTokenResponse>
        {
            Data = response,
            Error = null
        });
    }

    [HttpGet("me/effective-access")]
    [Authorize]
    [ProducesResponseType(typeof(EffectiveAccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffectiveAccess(CancellationToken cancellationToken)
    {
        var result = await authService.GetEffectiveAccessAsync(User.GetUserId(), cancellationToken);
        return Ok(new EffectiveAccessResponse
        {
            ProfileId = result.ProfileId,
            Roles = result.Roles,
            EffectivePermissions = result.EffectivePermissions,
            PrimaryRole = result.PrimaryRole,
            DefaultHome = result.DefaultHome,
            IsStaff = result.IsStaff
        });
    }

    private static AuthSessionResponse Map(MedicalCenter.Application.DTOs.AuthResponse result) => new()
    {
        Session = new AuthTokenResponse
        {
            AccessToken = result.Session.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.Session.ExpiresInSeconds
        },
        User = new AuthUserResponse
        {
            Id = result.UserId,
            Email = result.Email
        }
    };
}
