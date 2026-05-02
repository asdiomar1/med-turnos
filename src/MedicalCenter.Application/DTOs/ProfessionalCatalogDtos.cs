namespace MedicalCenter.Application.DTOs;

public sealed record MedicoSummaryDto(Guid Id, string Nombre);

public sealed record ReferenteSummaryDto(int Id, string Nombre, string Tipo, bool Activo, int Orden, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record OperadorCamaraSummaryDto(Guid Id, string Nombre, bool IsActive);

public sealed record CampoConfigSummaryDto(Guid Id, string Nombre, string Tipo, int Orden, DateTimeOffset CreatedAt);
