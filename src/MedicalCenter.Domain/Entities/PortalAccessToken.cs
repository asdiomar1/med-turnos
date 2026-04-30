using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class PortalAccessToken : Entity<Guid>
{
    private PortalAccessToken() { }

    public PortalAccessToken(
        Guid id,
        Guid pacienteId,
        string purpose,
        string deliveryChannel,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid? issuedBy)
    {
        Id = id;
        PacienteId = pacienteId;
        Purpose = purpose;
        DeliveryChannel = deliveryChannel;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        IssuedBy = issuedBy;
        IssuedAt = DateTimeOffset.UtcNow;
        Metadata = "{}";
    }

    public Guid PacienteId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string DeliveryChannel { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public Guid? IssuedBy { get; private set; }
    public string? IssuedToMasked { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public string Metadata { get; private set; } = "{}";

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && RevokedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now)
    {
        UsedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
    }

    public void RegisterAttempt(DateTimeOffset now)
    {
        AttemptCount++;
        LastAttemptAt = now;
    }

    public void SetIssuedToMasked(string? issuedToMasked)
    {
        IssuedToMasked = string.IsNullOrWhiteSpace(issuedToMasked) ? null : issuedToMasked.Trim();
    }
}
