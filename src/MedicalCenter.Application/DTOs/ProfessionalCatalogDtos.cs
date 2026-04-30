namespace MedicalCenter.Application.DTOs;

public sealed record MedicoSummaryDto(int Id, string Nombre, bool Activo, int Orden, DateTimeOffset CreatedAt, Guid? PerfilId);

public sealed record ReferenteSummaryDto(int Id, string Nombre, string Tipo, bool Activo, int Orden, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record OperadorCamaraSummaryDto(Guid Id, string Nombre, bool IsActive);

public sealed record CampoConfigSummaryDto(Guid Id, string Nombre, string Tipo, int Orden, DateTimeOffset CreatedAt);
