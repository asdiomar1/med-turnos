namespace MedicalCenter.Application.DTOs;

public sealed record TokenEnvelope(string AccessToken, string JwtId, int ExpiresInSeconds);
