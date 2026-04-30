using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken() { }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, string jwtId)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        JwtId = jwtId;
        Status = RefreshTokenStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string JwtId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public RefreshTokenStatus Status { get; private set; }

    public bool IsActive(DateTimeOffset now) => Status == RefreshTokenStatus.Active && ExpiresAt > now;

    public void Revoke(DateTimeOffset now)
    {
        if (Status != RefreshTokenStatus.Active)
        {
            return;
        }

        Status = RefreshTokenStatus.Revoked;
        RevokedAt = now;
    }

    public void Rotate(Guid newTokenId, DateTimeOffset now)
    {
        ReplacedByTokenId = newTokenId;
        RevokedAt = now;
        Status = RefreshTokenStatus.Rotated;
    }

    public void MarkExpired()
    {
        if (Status == RefreshTokenStatus.Active)
        {
            Status = RefreshTokenStatus.Expired;
        }
    }
}
