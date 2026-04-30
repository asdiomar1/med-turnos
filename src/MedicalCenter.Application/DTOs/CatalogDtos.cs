namespace MedicalCenter.Application.DTOs;

public sealed record CondicionIvaSummaryDto(
    int Id,
    string Nombre,
    bool Activo,
    int Orden,
    DateTimeOffset CreatedAt);

public sealed record ObraSocialSummaryDto(
    int Id,
    string Nombre,
    bool Activa,
    bool TieneConvenio,
    int Orden,
    string? Abreviatura,
    DateTimeOffset CreatedAt);
