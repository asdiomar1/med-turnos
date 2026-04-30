namespace MedicalCenter.Application.DTOs;

public sealed record UserPreferencesSummary(
    Guid UserId,
    string Theme,
    string? CustomColorsJson,
    string TurnosLayout,
    decimal FontScale,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateUserPreferencesCommand(
    Guid UserId,
    string? Theme,
    string? CustomColorsJson,
    string? TurnosLayout,
    decimal? FontScale);
