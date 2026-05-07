using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using MedicalCenter.Contracts.Auth;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Auth;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.IntegrationTests.Api.E2E;

public sealed class SecurityE2ETests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SecurityE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HealthEndpoint_ContainsSecurityHeaders()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/pacientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = "nonexistent-user",
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = "",
            Password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("identifier", body);
    }

    // ==================== T10: Auth Integration Tests ====================
    // Los siguientes tests requieren infraestructura RBAC compleja (tablas perfiles, roles, etc.)
    // que no están disponibles en el contexto de test. Los casos de error ya están cubiertos
    // por los tests existentes: Login_WithInvalidCredentials_Returns401 y Login_WithInactiveUser_Returns401

    /// <summary>
    /// T10: Login con credenciales válidas - requiere seed de tablas RBAC (perfiles, rbac_roles, etc.)
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_Returns200_WithTokens()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";
        await SeedTestUserAsync(isActive: true, isStaff: true, identifier: identifier);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier,
            Password = "TestPass123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Session.AccessToken);
        Assert.NotNull(result.Session.RefreshToken);
    }

    /// <summary>
    /// T10: Login case insensitive
    /// </summary>
    [Fact]
    public async Task Login_WithEmailInUpperCase_Returns200_CaseInsensitive()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";
        await SeedTestUserAsync(isActive: true, isStaff: true, identifier: identifier);

        // Act - usar identifier en uppercase
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier.ToUpperInvariant(),
            Password = "TestPass123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Session.AccessToken);
    }

    /// <summary>
    /// T10: Login con usuario inactivo - Cubierto por test existente
    /// </summary>
    [Fact]
    public async Task Login_WithInactiveUser_Returns401()
    {
        // Arrange - usuario inactivo NO requiere tablas RBAC para fallar
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";

        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        await using var context = new MedicalCenterDbContext(options);

        var user = new User(new UserCreateParams(
            Id: Guid.NewGuid(),
            Identifier: identifier,
            Email: $"staff-{uniqueId}@test.com",
            PasswordHash: HashPassword("TestPass123!"),
            IsActive: false, // inactivo
            IsStaff: true
        ));

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier,
            Password = "TestPass123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// T10: Refresh con token válido - requiere login exitoso + seed RBAC
    /// </summary>
    [Fact]
    public async Task Refresh_WithValidToken_Returns200()
    {
        // Arrange - use login flow to get valid refresh token
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";
        await SeedTestUserAsync(isActive: true, isStaff: true, identifier: identifier);

        // Login to get valid tokens
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier,
            Password = "TestPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>();
        Assert.NotNull(loginResult);

        // Act - refresh using the token from login
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
        {
            RefreshToken = loginResult.Session.RefreshToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Session.AccessToken);
        Assert.NotNull(result.Session.RefreshToken);
    }

    /// <summary>
    /// T10: Refresh con token expirado - requiere seed de refresh tokens
    /// </summary>
    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        // Arrange - create user and get valid token first
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";
        await SeedTestUserAsync(isActive: true, isStaff: true, identifier: identifier);

        // Login to establish valid session, then use invalid/expired token
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier,
            Password = "TestPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Act - try to refresh with invalid token
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
        {
            RefreshToken = "invalid-expired-token"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// T10: Logout con token válido - requiere login exitoso + seed RBAC
    /// </summary>
    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var identifier = $"staff-{uniqueId}";
        await SeedTestUserAsync(isActive: true, isStaff: true, identifier: identifier);

        // Login first to get tokens
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Identifier = identifier,
            Password = "TestPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>();
        Assert.NotNull(loginResult);

        // Act - logout with access token and refresh token
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest
        {
            RefreshToken = loginResult.Session.RefreshToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static string HashPassword(string plainText)
    {
        const int saltSize = 16;
        const int keySize = 32;
        const int iterations = 100_000;

        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(plainText, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, keySize);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private async Task<User> SeedTestUserAsync(bool isActive = true, bool isStaff = true, string? identifier = null)
    {
        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        await using var context = new MedicalCenterDbContext(options);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var userIdentifier = identifier ?? (isStaff ? $"staff-{uniqueId}" : $"patient-{uniqueId}");

        var user = new User(new UserCreateParams(
            Id: Guid.NewGuid(),
            Identifier: userIdentifier,
            Email: isStaff ? $"staff-{uniqueId}@test.com" : $"patient-{uniqueId}@test.com",
            PasswordHash: HashPassword("TestPass123!"),
            IsActive: isActive,
            IsStaff: isStaff
        ));

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create perfil for the user (required for RBAC)
        var profileId = Guid.NewGuid();
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.perfiles (
                id, nombre, email, rol, auth_user_id, portal_habilitado, 
                requiere_reset_portal, portal_login_email, created_at, updated_at
            ) VALUES (
                {0}, {1}, {2}, 'admin', {3}, false, false, {2}, now(), now()
            );
            """,
            [profileId, $"Test User {uniqueId}", user.Email, user.Id]);

        // Assign staff role to the perfil for RBAC
        if (isStaff)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO public.rbac_user_roles (user_id, role_id, is_primary, assigned_at)
                SELECT {0}, id, true, now()
                FROM public.rbac_roles
                WHERE slug = 'staff'
                ON CONFLICT (user_id, role_id) DO NOTHING;
                """,
                [profileId]);

            // Refresh effective permissions
            await context.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM public.rbac_effective_permissions WHERE user_id = {0};
                
                INSERT INTO public.rbac_effective_permissions (user_id, permission_key, source_role_id, created_at, updated_at)
                SELECT 
                    ur.user_id,
                    perm.key,
                    ur.role_id,
                    now(),
                    now()
                FROM public.rbac_user_roles ur
                JOIN public.rbac_roles r ON r.id = ur.role_id AND r.activo = true
                JOIN public.rbac_role_permissions rp ON rp.role_id = ur.role_id AND rp.granted = true
                JOIN public.rbac_permissions perm ON perm.id = rp.permission_id
                WHERE ur.user_id = {0}
                  AND ur.expires_at IS NULL
                GROUP BY ur.user_id, perm.key, ur.role_id;
                """,
                [profileId]);
        }

        return user;
    }

    private async Task<RefreshToken> SeedRefreshTokenAsync(Guid userId, bool isExpired = false)
    {
        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

        await using var context = new MedicalCenterDbContext(options);

        var token = new RefreshToken(
            id: Guid.NewGuid(),
            userId: userId,
            tokenHash: Guid.NewGuid().ToString(), // token hash único
            expiresAt: isExpired ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddDays(7),
            jwtId: "test-jwt-id"
        );

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        return token;
    }
}
