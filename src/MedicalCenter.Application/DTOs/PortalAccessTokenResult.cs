namespace MedicalCenter.Application.DTOs;

public sealed record PortalAccessTokenResult(
    Guid TokenId,
    string Purpose,
    string DeliveryChannel,
    DateTimeOffset ExpiresAt,
    string? TokenPlain);
