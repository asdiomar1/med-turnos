namespace MedicalCenter.Application.DTOs;

public sealed record DiasLaborablesConfigDto(
    string Key,
    IReadOnlyCollection<short> DiasSemana);

public sealed record WhatsappMessageSettingDto(
    string Key,
    string Label,
    string? Description,
    string MessageText,
    bool Active,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
