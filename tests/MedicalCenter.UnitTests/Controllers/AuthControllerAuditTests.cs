using MedicalCenter.Api.Controllers.Auth;
using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Contracts.Auth;
using MedicalCenter.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalCenter.UnitTests.Controllers;

public sealed class AuthControllerAuditTests
{
    [Fact]
    public async Task ChangeMyPassword_WithAuthenticatedUser_ReturnsNoContent_AndLogsSuccess()
    {
        var userId = Guid.NewGuid();
        var authService = new FakeAuthService(shouldFail: false);
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = new AuthController(authService, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(userId)
        };

        var result = await controller.ChangeMyPassword(
            new ChangeMyPasswordRequest { CurrentPassword = "actual123", NewPassword = "nueva1234" },
            CancellationToken.None);

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
        Assert.Equal(userId, authService.ChangeOwnPasswordUserId);
        Assert.Equal("actual123", authService.CurrentPassword);
        Assert.Equal("nueva1234", authService.NewPassword);
        Assert.Single(auditLogger.Events);
        var evt = auditLogger.Events[0];
        Assert.Equal("password_changed", evt.EventType);
        Assert.Equal(userId.ToString(), evt.UserId);
        Assert.Equal("/api/v1/auth/me/password", evt.Path);
        Assert.Contains("refresh tokens revoked", evt.Message);
        Assert.Contains("access tokens remain valid until expiry", evt.Message);
    }

    [Fact]
    public async Task ChangeMyPassword_WhenServiceRejects_LogsFailureAndRethrows()
    {
        var userId = Guid.NewGuid();
        var authService = new FakeAuthService(shouldFail: false)
        {
            ChangeOwnPasswordException = new UnauthorizedException()
        };
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = new AuthController(authService, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(userId)
        };

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            controller.ChangeMyPassword(
                new ChangeMyPasswordRequest { CurrentPassword = "actual123", NewPassword = "nueva1234" },
                CancellationToken.None));

        Assert.Single(auditLogger.Events);
        var evt = auditLogger.Events[0];
        Assert.Equal("password_change_failed", evt.EventType);
        Assert.Equal(userId.ToString(), evt.UserId);
        Assert.Equal("/api/v1/auth/me/password", evt.Path);
        Assert.Contains("Failed password change", evt.Message);
    }

    [Fact]
    public async Task ChangeMyPassword_WithoutResolvablePrincipal_LogsFailureAndRethrows()
    {
        var authService = new FakeAuthService(shouldFail: false);
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = new AuthController(authService, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.ChangeMyPassword(
                new ChangeMyPasswordRequest { CurrentPassword = "actual123", NewPassword = "nueva1234" },
                CancellationToken.None));

        Assert.Single(auditLogger.Events);
        var evt = auditLogger.Events[0];
        Assert.Equal("password_change_failed", evt.EventType);
        Assert.Null(evt.UserId);
        Assert.Equal("/api/v1/auth/me/password", evt.Path);
        Assert.Contains("Failed password change", evt.Message);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_LogsAuthFailure()
    {
        var authService = new FakeAuthService(shouldFail: true);
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = new AuthController(authService, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            controller.Login(new LoginRequest { Identifier = "bad-user", Password = "bad-pass" }, CancellationToken.None));

        Assert.Single(auditLogger.Events);
        var evt = auditLogger.Events[0];
        Assert.Equal("auth_failure", evt.EventType);
        Assert.Equal("bad-user", evt.UserId);
        Assert.Contains("Failed login attempt", evt.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_DoesNotLogAuthFailure()
    {
        var authService = new FakeAuthService(shouldFail: false);
        var auditLogger = new FakeSecurityAuditLogger();
        var controller = new AuthController(authService, auditLogger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Login(new LoginRequest { Identifier = "good-user", Password = "good-pass" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(auditLogger.Events);
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth"));

        return context;
    }

    private sealed class FakeAuthService(bool shouldFail) : IAuthService
    {
        public Exception? ChangeOwnPasswordException { get; init; }
        public Guid? ChangeOwnPasswordUserId { get; private set; }
        public string? CurrentPassword { get; private set; }
        public string? NewPassword { get; private set; }

        public Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
        {
            ChangeOwnPasswordUserId = userId;
            CurrentPassword = currentPassword;
            NewPassword = newPassword;

            if (ChangeOwnPasswordException is not null)
            {
                throw ChangeOwnPasswordException;
            }

            return Task.CompletedTask;
        }

        public Task<AuthResponse> LoginAsync(string identifier, string password, CancellationToken cancellationToken) =>
            shouldFail
                ? throw new UnauthorizedException()
                : Task.FromResult(new AuthResponse(new TokenEnvelope("token", Guid.NewGuid().ToString(), 3600), "refresh", Guid.NewGuid(), "test@test.com"));

        public Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
            Task.FromResult(new AuthResponse(new TokenEnvelope("token", Guid.NewGuid().ToString(), 3600), "refresh", Guid.NewGuid(), "test@test.com"));

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<EffectiveAccessResult> GetEffectiveAccessAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(new EffectiveAccessResult(Guid.NewGuid(), [], [], "user", "/", false));

        public Task<PortalActivationResult> ActivatePortalAsync(string token, string loginIdentifier, string password, CancellationToken cancellationToken) =>
            Task.FromResult(new PortalActivationResult(true, loginIdentifier));

        public Task<PortalRecoveryResult> RequestPortalRecoveryAsync(string documentoIdentidad, CancellationToken cancellationToken) =>
            Task.FromResult(new PortalRecoveryResult(true, false));

        public Task<PortalAccessTokenResult> CreatePortalAccessTokenAsync(Guid pacienteId, string purpose, string deliveryChannel, Guid? issuedBy, CancellationToken cancellationToken) =>
            Task.FromResult(new PortalAccessTokenResult(Guid.NewGuid(), purpose, deliveryChannel, DateTimeOffset.UtcNow.AddHours(12), "123456"));
    }

    private sealed class FakeSecurityAuditLogger : ISecurityAuditLogger
    {
        public List<SecurityEvent> Events { get; } = [];
        public void LogAsync(SecurityEvent securityEvent) => Events.Add(securityEvent);
    }
}
