namespace MedicalCenter.Application.DTOs;

public sealed record AuthResponse(
    TokenEnvelope Session,
    string RefreshToken,
    Guid UserId,
    string? Email);
