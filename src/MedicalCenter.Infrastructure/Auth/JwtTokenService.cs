using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedicalCenter.Application.Abstractions.Auth;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MedicalCenter.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public TokenEnvelope CreateAccessToken(User user)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var isStaff = user.Roles.Any(role => role.IsStaff);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("identifier", user.Identifier),
            new("is_staff", isStaff.ToString().ToLowerInvariant())
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Code)));
        claims.AddRange(user.Roles.SelectMany(role => role.Permissions).Distinct(StringComparer.OrdinalIgnoreCase).Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expiresAt, signingCredentials: creds);
        return new TokenEnvelope(new JwtSecurityTokenHandler().WriteToken(token), jwtId, _options.AccessTokenExpirationMinutes * 60);
    }

    public string CreateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string CreateNumericCode(int digits)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(digits);

        var maxExclusive = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, maxExclusive);
        return value.ToString($"D{digits}");
    }

    public string ComputeRefreshTokenHash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
